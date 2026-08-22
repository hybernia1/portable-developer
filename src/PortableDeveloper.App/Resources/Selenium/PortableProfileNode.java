package portabledeveloper.selenium;

import java.io.IOException;
import java.io.InputStream;
import java.net.URI;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.StandardCopyOption;
import java.nio.file.attribute.BasicFileAttributes;
import java.time.Duration;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.openqa.selenium.Capabilities;
import org.openqa.selenium.ImmutableCapabilities;
import org.openqa.selenium.NoSuchSessionException;
import org.openqa.selenium.WebDriverException;
import org.openqa.selenium.grid.config.Config;
import org.openqa.selenium.grid.data.CreateSessionRequest;
import org.openqa.selenium.grid.data.CreateSessionResponse;
import org.openqa.selenium.grid.data.NodeId;
import org.openqa.selenium.grid.data.NodeStatus;
import org.openqa.selenium.grid.data.Session;
import org.openqa.selenium.grid.log.LoggingOptions;
import org.openqa.selenium.grid.node.HealthCheck;
import org.openqa.selenium.grid.node.Node;
import org.openqa.selenium.grid.node.config.NodeOptions;
import org.openqa.selenium.grid.node.local.LocalNodeFactory;
import org.openqa.selenium.grid.security.Secret;
import org.openqa.selenium.grid.security.SecretOptions;
import org.openqa.selenium.grid.server.BaseServerOptions;
import org.openqa.selenium.internal.Either;
import org.openqa.selenium.io.TemporaryFilesystem;
import org.openqa.selenium.remote.SessionId;
import org.openqa.selenium.remote.http.HttpMethod;
import org.openqa.selenium.remote.http.HttpRequest;
import org.openqa.selenium.remote.http.HttpResponse;
import org.openqa.selenium.remote.tracing.Tracer;

public final class PortableProfileNode extends Node {
  private static final String PROFILE_CAPABILITY = "portable:profile";
  private static final Pattern SAFE_ID = Pattern.compile("^[a-fA-F0-9]{32}$");
  private static final Pattern DELETE_SESSION = Pattern.compile("^/session/([^/]+)$");
  private final Node node;
  private final Path portableRoot;
  private final ConcurrentHashMap<String, Path> sessionCopies = new ConcurrentHashMap<>();

  private PortableProfileNode(
      Tracer tracer,
      NodeId nodeId,
      URI uri,
      Secret registrationSecret,
      Duration sessionTimeout,
      Node node,
      Path portableRoot) {
    super(tracer, nodeId, uri, registrationSecret, sessionTimeout);
    this.node = node;
    this.portableRoot = portableRoot;
    var cleaner = Executors.newSingleThreadScheduledExecutor(runnable -> {
      Thread thread = new Thread(runnable, "portable-profile-cleaner");
      thread.setDaemon(true);
      return thread;
    });
    cleaner.scheduleWithFixedDelay(this::cleanupEndedSessions, 30, 30, TimeUnit.SECONDS);
  }

  public static Node create(Config config) {
    Node delegate = LocalNodeFactory.create(config);
    Path root = Path.of(requiredEnvironment("PORTABLE_DEVELOPER_ROOT")).toAbsolutePath().normalize();
    NodeOptions nodeOptions = new NodeOptions(config);
    return new PortableProfileNode(
        new LoggingOptions(config).getTracer(),
        delegate.getId(),
        new BaseServerOptions(config).getExternalUri(),
        new SecretOptions(config).getRegistrationSecret(),
        nodeOptions.getSessionTimeout(),
        delegate,
        root);
  }

  @Override
  public Either<WebDriverException, CreateSessionResponse> newSession(CreateSessionRequest request) {
    Object requestedProfile = request.getDesiredCapabilities().getCapability(PROFILE_CAPABILITY);
    if (requestedProfile == null) {
      return node.newSession(request);
    }

    Path workingCopy = null;
    try {
      String profileId = requestedProfile.toString();
      validateId(profileId);
      ProfileDefinition profile = readProfile(profileId);
      String requestedBrowser = request.getDesiredCapabilities().getBrowserName();
      if (!profile.browser.equalsIgnoreCase(requestedBrowser)) {
        throw new WebDriverException(
            "Profile '" + profileId + "' belongs to " + profile.browser +
            " but the requested browser is " + requestedBrowser + ".");
      }

      String token = UUID.randomUUID().toString().replace("-", "");
      workingCopy = safeResolve(Path.of("temp", "selenium-profiles", token));
      copyDirectory(profile.master, workingCopy);
      Capabilities capabilities = withWorkingProfile(request.getDesiredCapabilities(), profile.browser, workingCopy);
      CreateSessionRequest updated = new CreateSessionRequest(
          request.getDownstreamDialects(), capabilities, request.getMetadata());
      Either<WebDriverException, CreateSessionResponse> result = node.newSession(updated);
      if (result.isRight()) {
        sessionCopies.put(result.right().getSession().getId().toString(), workingCopy);
        return result;
      }

      deleteDirectory(workingCopy);
      return result;
    } catch (Exception exception) {
      if (workingCopy != null) {
        deleteQuietly(workingCopy);
      }
      return Either.left(exception instanceof WebDriverException
          ? (WebDriverException) exception
          : new WebDriverException("Portable profile preparation failed: " + exception.getMessage(), exception));
    }
  }

  @Override
  public HttpResponse executeWebDriverCommand(HttpRequest request) {
    HttpResponse response = node.executeWebDriverCommand(request);
    if (request.getMethod() == HttpMethod.DELETE) {
      Matcher matcher = DELETE_SESSION.matcher(request.getUri());
      if (matcher.matches()) {
        cleanupSession(matcher.group(1));
      }
    }
    return response;
  }

  @Override
  public void stop(SessionId id) throws NoSuchSessionException {
    try {
      node.stop(id);
    } finally {
      cleanupSession(id.toString());
    }
  }

  @Override public Session getSession(SessionId id) throws NoSuchSessionException { return node.getSession(id); }
  @Override public HttpResponse uploadFile(HttpRequest request, SessionId id) { return node.uploadFile(request, id); }
  @Override public HttpResponse downloadFile(HttpRequest request, SessionId id) { return node.downloadFile(request, id); }
  @Override public TemporaryFilesystem getDownloadsFilesystem(SessionId id) throws IOException { return node.getDownloadsFilesystem(id); }
  @Override public TemporaryFilesystem getUploadsFilesystem(SessionId id) throws IOException { return node.getUploadsFilesystem(id); }
  @Override public boolean isSessionOwner(SessionId id) { return node.isSessionOwner(id); }
  @Override public boolean tryAcquireConnection(SessionId id) { return node.tryAcquireConnection(id); }
  @Override public void releaseConnection(SessionId id) { node.releaseConnection(id); }
  @Override public boolean isSupporting(Capabilities capabilities) { return node.isSupporting(capabilities); }
  @Override public NodeStatus getStatus() { return node.getStatus(); }
  @Override public HealthCheck getHealthCheck() { return node.getHealthCheck(); }
  @Override public void drain() { node.drain(); }
  @Override public boolean isReady() { return node.isReady(); }

  private ProfileDefinition readProfile(String id) throws IOException {
    Path profileRoot = safeResolve(Path.of("profiles", "selenium", id));
    Path propertiesPath = profileRoot.resolve("profile.properties");
    Path master = profileRoot.resolve("master").normalize();
    if (!Files.isRegularFile(propertiesPath, LinkOption.NOFOLLOW_LINKS)
        || !Files.isDirectory(master, LinkOption.NOFOLLOW_LINKS)
        || Files.isSymbolicLink(master)) {
      throw new IOException("The requested master profile is missing or unsafe.");
    }

    Properties properties = new Properties();
    try (InputStream input = Files.newInputStream(propertiesPath)) {
      properties.load(input);
    }
    if (!id.equals(properties.getProperty("id"))) {
      throw new IOException("The master profile metadata does not match its directory.");
    }
    return new ProfileDefinition(properties.getProperty("browser", ""), master);
  }

  private static Capabilities withWorkingProfile(Capabilities original, String browser, Path copy) {
    Map<String, Object> capabilities = new HashMap<>(original.asMap());
    if (browser.equalsIgnoreCase("chrome") || browser.equalsIgnoreCase("MicrosoftEdge")) {
      String optionsKey = browser.equalsIgnoreCase("chrome") ? "goog:chromeOptions" : "ms:edgeOptions";
      Map<String, Object> options = mutableMap(capabilities.get(optionsKey));
      List<String> arguments = mutableArguments(options.get("args"));
      arguments.removeIf(argument -> argument.startsWith("user-data-dir=") || argument.startsWith("--user-data-dir="));
      arguments.add("--user-data-dir=" + copy.toAbsolutePath());
      options.put("args", arguments);
      capabilities.put(optionsKey, options);
    } else if (browser.equalsIgnoreCase("firefox")) {
      Map<String, Object> options = mutableMap(capabilities.get("moz:firefoxOptions"));
      List<String> arguments = mutableArguments(options.get("args"));
      if (arguments.stream().anyMatch(argument -> argument.equals("-profile") || argument.equals("--profile"))) {
        throw new WebDriverException("The request already defines a Firefox profile argument.");
      }
      arguments.add("-profile");
      arguments.add(copy.toAbsolutePath().toString());
      options.put("args", arguments);
      capabilities.put("moz:firefoxOptions", options);
    } else {
      throw new WebDriverException("Unsupported browser for a portable profile: " + browser);
    }
    return new ImmutableCapabilities(capabilities);
  }

  private static Map<String, Object> mutableMap(Object value) {
    Map<String, Object> result = new HashMap<>();
    if (value instanceof Map<?, ?> source) {
      source.forEach((key, item) -> result.put(key.toString(), item));
    }
    return result;
  }

  private static List<String> mutableArguments(Object value) {
    List<String> result = new ArrayList<>();
    if (value instanceof List<?> source) {
      source.forEach(item -> result.add(item.toString()));
    }
    return result;
  }

  private static void copyDirectory(Path source, Path destination) throws IOException {
    Files.createDirectories(destination);
    Files.walkFileTree(source, new SimpleFileVisitor<>() {
      @Override
      public FileVisitResult preVisitDirectory(Path directory, BasicFileAttributes attributes) throws IOException {
        if (Files.isSymbolicLink(directory) || attributes.isSymbolicLink() || attributes.isOther()) {
          throw new IOException("Master profiles cannot contain links or special directories.");
        }
        Files.createDirectories(destination.resolve(source.relativize(directory)));
        return FileVisitResult.CONTINUE;
      }

      @Override
      public FileVisitResult visitFile(Path file, BasicFileAttributes attributes) throws IOException {
        if (Files.isSymbolicLink(file) || attributes.isSymbolicLink() || attributes.isOther()) {
          throw new IOException("Master profiles cannot contain links or special files.");
        }
        Files.copy(file, destination.resolve(source.relativize(file)), StandardCopyOption.COPY_ATTRIBUTES);
        destination.resolve(source.relativize(file)).toFile().setWritable(true, false);
        return FileVisitResult.CONTINUE;
      }
    });
  }

  private void cleanupEndedSessions() {
    sessionCopies.keySet().forEach(id -> {
      try {
        node.getSession(new SessionId(id));
      } catch (NoSuchSessionException exception) {
        cleanupSession(id);
      }
    });
  }

  private void cleanupSession(String id) {
    Path copy = sessionCopies.remove(id);
    if (copy != null) {
      deleteQuietly(copy);
    }
  }

  private static void deleteQuietly(Path path) {
    try {
      deleteDirectory(path);
    } catch (IOException exception) {
      System.err.println("[PortableProfileNode] Could not remove session profile: " + exception.getMessage());
    }
  }

  private static void deleteDirectory(Path root) throws IOException {
    if (!Files.exists(root, LinkOption.NOFOLLOW_LINKS)) {
      return;
    }
    Files.walkFileTree(root, new SimpleFileVisitor<>() {
      @Override
      public FileVisitResult visitFile(Path file, BasicFileAttributes attributes) throws IOException {
        Files.delete(file);
        return FileVisitResult.CONTINUE;
      }

      @Override
      public FileVisitResult postVisitDirectory(Path directory, IOException exception) throws IOException {
        if (exception != null) throw exception;
        Files.delete(directory);
        return FileVisitResult.CONTINUE;
      }
    });
  }

  private Path safeResolve(Path relative) throws IOException {
    Path resolved = portableRoot.resolve(relative).normalize();
    if (!resolved.startsWith(portableRoot)) {
      throw new IOException("A portable profile path escaped the application root.");
    }
    return resolved;
  }

  private static void validateId(String id) {
    if (!SAFE_ID.matcher(id).matches()) {
      throw new WebDriverException("The portable profile identifier is invalid.");
    }
  }

  private static String requiredEnvironment(String name) {
    String value = System.getenv(name);
    if (value == null || value.isBlank()) {
      throw new IllegalStateException(name + " is not set.");
    }
    return value;
  }

  private record ProfileDefinition(String browser, Path master) {}
}

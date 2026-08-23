package portabledeveloper.selenium;

import java.io.IOException;
import java.io.InputStream;
import java.lang.reflect.Type;
import java.net.URI;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.StandardCopyOption;
import java.nio.file.StandardOpenOption;
import java.nio.charset.StandardCharsets;
import java.nio.file.attribute.BasicFileAttributes;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.GeneralSecurityException;
import java.time.Duration;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.HashSet;
import java.util.HexFormat;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import javax.crypto.Cipher;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.SecretKeySpec;
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
import org.openqa.selenium.json.Json;
import org.openqa.selenium.json.TypeToken;
import org.openqa.selenium.remote.SessionId;
import org.openqa.selenium.remote.http.Contents;
import org.openqa.selenium.remote.http.HttpMethod;
import org.openqa.selenium.remote.http.HttpRequest;
import org.openqa.selenium.remote.http.HttpResponse;
import org.openqa.selenium.remote.tracing.Tracer;

public final class PortableProfileNode extends Node {
  private static final String PROFILE_CAPABILITY = "portable:profile";
  private static final String VAULT_CAPABILITY = "portable:vault";
  private static final Pattern SAFE_ID = Pattern.compile("^[a-fA-F0-9]{32}$");
  private static final Pattern SAFE_DOMAIN = Pattern.compile(
      "^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\\.)*" +
      "[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$");
  private static final Pattern DELETE_SESSION = Pattern.compile("^/session/([^/]+)$");
  private static final Json JSON = new Json();
  private static final Type COOKIE_LIST_TYPE = new TypeToken<List<Map<String, Object>>>() {}.getType();
  private static final Type VAULT_ENVELOPE_TYPE = new TypeToken<Map<String, Object>>() {}.getType();
  private final Node node;
  private final Path portableRoot;
  private final boolean downloadsEnabled;
  private final Path downloadRoot;
  private final ConcurrentHashMap<String, Path> sessionCopies = new ConcurrentHashMap<>();

  private PortableProfileNode(
      Tracer tracer,
      NodeId nodeId,
      URI uri,
      Secret registrationSecret,
      Duration sessionTimeout,
      Node node,
      Path portableRoot,
      boolean downloadsEnabled,
      Path downloadRoot) {
    super(tracer, nodeId, uri, registrationSecret, sessionTimeout);
    this.node = node;
    this.portableRoot = portableRoot;
    this.downloadsEnabled = downloadsEnabled;
    this.downloadRoot = downloadRoot;
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
    boolean downloadsEnabled = parseBooleanEnvironment("PORTABLE_DEVELOPER_DOWNLOADS_ENABLED");
    Path downloadRoot = downloadsEnabled
        ? validateDownloadRoot(root, requiredEnvironment("PORTABLE_DEVELOPER_DOWNLOADS"))
        : null;
    NodeOptions nodeOptions = new NodeOptions(config);
    return new PortableProfileNode(
        new LoggingOptions(config).getTracer(),
        delegate.getId(),
        new BaseServerOptions(config).getExternalUri(),
        new SecretOptions(config).getRegistrationSecret(),
        nodeOptions.getSessionTimeout(),
        delegate,
        root,
        downloadsEnabled,
        downloadRoot);
  }

  @Override
  public Either<WebDriverException, CreateSessionResponse> newSession(CreateSessionRequest request) {
    Object requestedProfile = request.getDesiredCapabilities().getCapability(PROFILE_CAPABILITY);
    Object requestedVault = request.getDesiredCapabilities().getCapability(VAULT_CAPABILITY);
    Path workingCopy = null;
    try {
      Capabilities capabilities = request.getDesiredCapabilities();
      if (requestedProfile != null) {
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
        disableProfileSync(profile.browser, workingCopy);
        capabilities = withWorkingProfile(
            capabilities, profile.browser, workingCopy, profile.profileDirectory);
      }

      String requestedBrowser = request.getDesiredCapabilities().getBrowserName();
      capabilities = withDownloadSettings(capabilities, requestedBrowser, downloadsEnabled, downloadRoot);

      List<Map<String, Object>> vaultCookies = null;
      if (requestedVault != null) {
        String vaultId = requestedVault.toString();
        validateId(vaultId);
        vaultCookies = readVault(vaultId);
      }

      CreateSessionRequest updated = new CreateSessionRequest(
          request.getDownstreamDialects(), capabilities, request.getMetadata());
      Either<WebDriverException, CreateSessionResponse> result = node.newSession(updated);
      if (result.isRight()) {
        SessionId sessionId = result.right().getSession().getId();
        try {
          configureChromiumDownloads(sessionId, requestedBrowser);
          if (vaultCookies != null) {
            injectCookies(sessionId, vaultCookies);
          }
          if (workingCopy != null) {
            sessionCopies.put(sessionId.toString(), workingCopy);
          }
          return result;
        } catch (Exception exception) {
          try {
            node.stop(sessionId);
          } catch (Exception ignored) {
            // The original cookie injection failure is more useful to the caller.
          }
          if (workingCopy != null) {
            deleteQuietly(workingCopy);
          }
          return Either.left(new WebDriverException(
              "Portable Selenium session initialization failed. The session was closed without returning it.", exception));
        }
      }

      if (workingCopy != null) {
        deleteDirectory(workingCopy);
      }
      return result;
    } catch (Exception exception) {
      if (workingCopy != null) {
        deleteQuietly(workingCopy);
      }
      return Either.left(exception instanceof WebDriverException
          ? (WebDriverException) exception
          : new WebDriverException("Portable Selenium session preparation failed: " + exception.getMessage(), exception));
    }
  }

  @Override
  public HttpResponse executeWebDriverCommand(HttpRequest request) {
    if (isDownloadPolicyOverride(request)) {
      HttpResponse denied = new HttpResponse().setStatus(403);
      denied.setContent(Contents.asJson(Map.of("value", Map.of(
          "error", "unsupported operation",
          "message", "Portable Developer owns the browser download policy. Change it in Selenium settings.",
          "stacktrace", ""))));
      return denied;
    }

    HttpResponse response = node.executeWebDriverCommand(request);
    if (request.getMethod() == HttpMethod.DELETE) {
      Matcher matcher = DELETE_SESSION.matcher(request.getUri());
      if (matcher.matches()) {
        cleanupSession(matcher.group(1));
      }
    }
    return response;
  }

  private static boolean isDownloadPolicyOverride(HttpRequest request) {
    if (request.getMethod() != HttpMethod.POST
        || (!request.getUri().contains("/cdp/execute")
            && !request.getUri().contains("/chromium/send_command"))) {
      return false;
    }

    try {
      Map<String, Object> payload = Contents.fromJson(request, VAULT_ENVELOPE_TYPE);
      Object command = payload == null ? null : payload.get("cmd");
      return command != null && command.toString().endsWith(".setDownloadBehavior");
    } catch (RuntimeException exception) {
      return false;
    }
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
    Path manifestPath = profileRoot.resolve("profile.manifest");
    Path master = profileRoot.resolve("master").normalize();
    if (!Files.isRegularFile(propertiesPath, LinkOption.NOFOLLOW_LINKS)
        || !Files.isRegularFile(manifestPath, LinkOption.NOFOLLOW_LINKS)
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
    String expectedManifestHash = properties.getProperty("manifestSha256", "");
    if (!sha256(manifestPath).equalsIgnoreCase(expectedManifestHash)) {
      throw new IOException("The master profile manifest is damaged.");
    }
    verifyManifest(manifestPath, master);
    return new ProfileDefinition(
        properties.getProperty("browser", ""),
        master,
        properties.getProperty("profileDirectory", ""));
  }

  private List<Map<String, Object>> readVault(String id) throws IOException {
    Path vaultRoot = safeResolve(Path.of("profiles", "selenium-vaults", id));
    Path envelopePath = vaultRoot.resolve("vault.json");
    Path keyPath = safeResolve(Path.of("state", "selenium-cookie-vault.key"));
    if (!Files.isDirectory(vaultRoot, LinkOption.NOFOLLOW_LINKS)
        || Files.isSymbolicLink(vaultRoot)
        || !isSafeRegularFile(envelopePath, 2, 10485760L)
        || !isSafeRegularFile(keyPath, 1, 128)) {
      throw new IOException("The requested cookie vault or its portable key is missing or unsafe.");
    }

    Map<String, Object> envelope;
    try {
      envelope = JSON.toType(Files.readString(envelopePath, StandardCharsets.UTF_8), VAULT_ENVELOPE_TYPE);
    } catch (RuntimeException exception) {
      throw new IOException("The encrypted cookie vault metadata is invalid.", exception);
    }
    if (envelope == null
        || integerValue(envelope.get("schemaVersion"), "schema version") != 2
        || !id.equals(requiredString(envelope, "id"))) {
      throw new IOException("The encrypted cookie vault metadata does not match its directory.");
    }
    int expectedCookieCount = integerValue(envelope.get("cookieCount"), "cookie count");
    if (expectedCookieCount < 1 || expectedCookieCount > 5000) {
      throw new IOException("The encrypted cookie vault contains an invalid cookie count.");
    }

    byte[] key = null;
    byte[] associatedData = null;
    byte[] nonce = null;
    byte[] tag = null;
    byte[] ciphertext = null;
    byte[] authenticatedCiphertext = null;
    byte[] plaintext = null;
    try {
      key = decodeFixed(Files.readString(keyPath, StandardCharsets.UTF_8).trim(), 32, "portable key");
      associatedData = decodeBounded(requiredString(envelope, "associatedData"), 1, 16384, "authentication data");
      nonce = decodeFixed(requiredString(envelope, "nonce"), 12, "nonce");
      tag = decodeFixed(requiredString(envelope, "tag"), 16, "authentication tag");
      ciphertext = decodeBounded(requiredString(envelope, "ciphertext"), 1, 5242880, "ciphertext");
      authenticatedCiphertext = new byte[ciphertext.length + tag.length];
      System.arraycopy(ciphertext, 0, authenticatedCiphertext, 0, ciphertext.length);
      System.arraycopy(tag, 0, authenticatedCiphertext, ciphertext.length, tag.length);

      Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
      cipher.init(Cipher.DECRYPT_MODE, new SecretKeySpec(key, "AES"), new GCMParameterSpec(128, nonce));
      cipher.updateAAD(associatedData);
      plaintext = cipher.doFinal(authenticatedCiphertext);
      List<Map<String, Object>> cookies = JSON.toType(
          new String(plaintext, StandardCharsets.UTF_8), COOKIE_LIST_TYPE);
      if (cookies == null || cookies.size() != expectedCookieCount) {
        throw new IOException("The decrypted cookie vault payload is invalid.");
      }
      return cookies;
    } catch (GeneralSecurityException | RuntimeException exception) {
      throw new IOException("The cookie vault could not be authenticated or decrypted.", exception);
    } finally {
      clear(key);
      clear(associatedData);
      clear(nonce);
      clear(tag);
      clear(ciphertext);
      clear(authenticatedCiphertext);
      clear(plaintext);
    }
  }

  private void injectCookies(SessionId sessionId, List<Map<String, Object>> cookies) throws IOException {
    Map<String, List<Map<String, Object>>> byDomain = new LinkedHashMap<>();
    for (Map<String, Object> cookie : cookies) {
      Object domainValue = cookie.get("domain");
      if (domainValue == null) {
        throw new IOException("A cookie vault item has no domain.");
      }
      String domain = domainValue.toString().toLowerCase();
      String host = domain.startsWith(".") ? domain.substring(1) : domain;
      if (domain.length() > 254 || host.length() > 253 || !SAFE_DOMAIN.matcher(host).matches()) {
        throw new IOException("A cookie vault item contains an unsafe domain.");
      }
      byDomain.computeIfAbsent(host, ignored -> new ArrayList<>()).add(cookie);
    }

    String sessionPath = "/session/" + sessionId;
    for (Map.Entry<String, List<Map<String, Object>>> group : byDomain.entrySet()) {
      boolean requiresHttps = group.getValue().stream()
          .anyMatch(cookie -> Boolean.TRUE.equals(cookie.get("secure")));
      boolean httpsSucceeded = navigateForCookieInjection(sessionPath, "https://" + group.getKey() + "/");
      if (!httpsSucceeded && !requiresHttps) {
        if (!navigateForCookieInjection(sessionPath, "http://" + group.getKey() + "/")) {
          throw new IOException("The browser could not open a cookie domain before injection.");
        }
      } else if (requiresHttps && !httpsSucceeded) {
        throw new IOException("The browser could not open a secure cookie domain before injection.");
      }

      for (Map<String, Object> cookie : group.getValue()) {
        HttpRequest addCookie = new HttpRequest(HttpMethod.POST, sessionPath + "/cookie");
        addCookie.setContent(Contents.asJson(Map.of("cookie", cookie)));
        HttpResponse response = node.executeWebDriverCommand(addCookie);
        if (response.getStatus() >= 400) {
          throw new IOException("The browser rejected a normalized cookie.");
        }
      }
    }

    HttpRequest reset = new HttpRequest(HttpMethod.POST, sessionPath + "/url");
    reset.setContent(Contents.asJson(Map.of("url", "about:blank")));
    node.executeWebDriverCommand(reset);
  }

  private static boolean isSafeRegularFile(Path path, long minimumBytes, long maximumBytes) throws IOException {
    return Files.isRegularFile(path, LinkOption.NOFOLLOW_LINKS)
        && !Files.isSymbolicLink(path)
        && Files.size(path) >= minimumBytes
        && Files.size(path) <= maximumBytes;
  }

  private static String requiredString(Map<String, Object> values, String name) throws IOException {
    Object value = values.get(name);
    if (!(value instanceof String text) || text.isBlank()) {
      throw new IOException("The cookie vault " + name + " is missing.");
    }
    return text;
  }

  private static int integerValue(Object value, String name) throws IOException {
    if (!(value instanceof Number number)) {
      throw new IOException("The cookie vault " + name + " is invalid.");
    }
    double numericValue = number.doubleValue();
    if (!Double.isFinite(numericValue) || numericValue != Math.rint(numericValue)
        || numericValue < Integer.MIN_VALUE || numericValue > Integer.MAX_VALUE) {
      throw new IOException("The cookie vault " + name + " is invalid.");
    }
    return (int) numericValue;
  }

  private static byte[] decodeFixed(String value, int expectedBytes, String name) throws IOException {
    byte[] decoded = decodeBounded(value, expectedBytes, expectedBytes, name);
    if (decoded.length != expectedBytes) {
      clear(decoded);
      throw new IOException("The cookie vault " + name + " is invalid.");
    }
    return decoded;
  }

  private static byte[] decodeBounded(String value, int minimumBytes, int maximumBytes, String name)
      throws IOException {
    try {
      byte[] decoded = Base64.getDecoder().decode(value);
      if (decoded.length < minimumBytes || decoded.length > maximumBytes) {
        clear(decoded);
        throw new IOException("The cookie vault " + name + " has an invalid size.");
      }
      return decoded;
    } catch (IllegalArgumentException exception) {
      throw new IOException("The cookie vault " + name + " is not valid Base64 data.", exception);
    }
  }

  private static void clear(byte[] value) {
    if (value != null) {
      Arrays.fill(value, (byte) 0);
    }
  }

  private boolean navigateForCookieInjection(String sessionPath, String url) {
    HttpRequest navigate = new HttpRequest(HttpMethod.POST, sessionPath + "/url");
    navigate.setContent(Contents.asJson(Map.of("url", url)));
    HttpResponse response = node.executeWebDriverCommand(navigate);
    return response.getStatus() < 400;
  }

  private static Capabilities withWorkingProfile(
      Capabilities original, String browser, Path copy, String profileDirectory) {
    Map<String, Object> capabilities = new HashMap<>(original.asMap());
    if (browser.equalsIgnoreCase("chrome") || browser.equalsIgnoreCase("MicrosoftEdge")) {
      String optionsKey = browser.equalsIgnoreCase("chrome") ? "goog:chromeOptions" : "ms:edgeOptions";
      Map<String, Object> options = mutableMap(capabilities.get(optionsKey));
      List<String> arguments = mutableArguments(options.get("args"));
      arguments.removeIf(argument -> argument.startsWith("user-data-dir=") || argument.startsWith("--user-data-dir="));
      arguments.removeIf(argument -> argument.startsWith("profile-directory=") || argument.startsWith("--profile-directory="));
      arguments.removeIf(argument -> argument.equals("--enable-sync") || argument.equals("--disable-sync"));
      arguments.add("--user-data-dir=" + copy.toAbsolutePath());
      arguments.add("--disable-sync");
      if (!profileDirectory.isBlank()) {
        if (!Path.of(profileDirectory).getFileName().toString().equals(profileDirectory)) {
          throw new WebDriverException("The Chromium profile directory name is invalid.");
        }
        arguments.add("--profile-directory=" + profileDirectory);
      }
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

  private static Capabilities withDownloadSettings(
      Capabilities original, String browser, boolean enabled, Path downloadRoot) {
    Map<String, Object> capabilities = new HashMap<>(original.asMap());
    if (browser.equalsIgnoreCase("chrome") || browser.equalsIgnoreCase("MicrosoftEdge")) {
      String optionsKey = browser.equalsIgnoreCase("chrome") ? "goog:chromeOptions" : "ms:edgeOptions";
      Map<String, Object> options = mutableMap(capabilities.get(optionsKey));
      Map<String, Object> preferences = mutableMap(options.get("prefs"));
      preferences.put("download.prompt_for_download", !enabled);
      preferences.put("download.directory_upgrade", enabled);
      preferences.put("safebrowsing.enabled", true);
      if (enabled) {
        preferences.put("download.default_directory", downloadRoot.toAbsolutePath().toString());
      } else {
        preferences.remove("download.default_directory");
      }
      options.put("prefs", preferences);
      capabilities.put(optionsKey, options);
    } else if (browser.equalsIgnoreCase("firefox")) {
      Map<String, Object> options = mutableMap(capabilities.get("moz:firefoxOptions"));
      Map<String, Object> preferences = mutableMap(options.get("prefs"));
      preferences.put("browser.download.folderList", 2);
      preferences.put("browser.download.useDownloadDir", enabled);
      preferences.put("browser.download.always_ask_before_handling_new_types", !enabled);
      preferences.put("browser.download.alwaysOpenPanel", false);
      preferences.put("browser.helperApps.neverAsk.saveToDisk", enabled ? "application/octet-stream" : "");
      if (enabled) {
        preferences.put("browser.download.dir", downloadRoot.toAbsolutePath().toString());
      } else {
        preferences.remove("browser.download.dir");
      }
      options.put("prefs", preferences);
      capabilities.put("moz:firefoxOptions", options);
    } else {
      throw new WebDriverException("Unsupported managed browser: " + browser);
    }
    return new ImmutableCapabilities(capabilities);
  }

  private void configureChromiumDownloads(SessionId sessionId, String browser) throws IOException {
    if (!browser.equalsIgnoreCase("chrome") && !browser.equalsIgnoreCase("MicrosoftEdge")) {
      return;
    }

    String vendor = browser.equalsIgnoreCase("chrome") ? "goog" : "ms";
    Map<String, Object> parameters = new HashMap<>();
    parameters.put("behavior", downloadsEnabled ? "allow" : "deny");
    if (downloadsEnabled) {
      parameters.put("downloadPath", downloadRoot.toAbsolutePath().toString());
      parameters.put("eventsEnabled", false);
    }
    HttpRequest command = new HttpRequest(
        HttpMethod.POST,
        "/session/" + sessionId + "/" + vendor + "/cdp/execute");
    command.setContent(Contents.asJson(Map.of(
        "cmd", "Browser.setDownloadBehavior",
        "params", parameters)));
    HttpResponse response = node.executeWebDriverCommand(command);
    if (response.getStatus() >= 400) {
      throw new IOException("The managed browser rejected the portable download policy.");
    }
  }

  private static void disableProfileSync(String browser, Path copy) throws IOException {
    if (!browser.equalsIgnoreCase("firefox")) {
      return;
    }

    Files.writeString(
        copy.resolve("user.js"),
        System.lineSeparator()
            + "// Portable Developer: session copies must not write back through Firefox Sync."
            + System.lineSeparator()
            + "user_pref(\"identity.fxaccounts.enabled\", false);"
            + System.lineSeparator(),
        StandardCharsets.UTF_8,
        StandardOpenOption.CREATE,
        StandardOpenOption.APPEND);
  }

  private static Path validateDownloadRoot(Path portableRoot, String value) {
    Path downloads = Path.of(value).toAbsolutePath().normalize();
    if (!downloads.startsWith(portableRoot)
        || !Files.isDirectory(downloads, LinkOption.NOFOLLOW_LINKS)
        || Files.isSymbolicLink(downloads)) {
      throw new IllegalStateException("PORTABLE_DEVELOPER_DOWNLOADS is not a safe directory inside the portable root.");
    }

    for (Path current = downloads; current != null && current.startsWith(portableRoot); current = current.getParent()) {
      if (Files.isSymbolicLink(current)) {
        throw new IllegalStateException("PORTABLE_DEVELOPER_DOWNLOADS must not contain symbolic links.");
      }
      if (current.equals(portableRoot)) {
        return downloads;
      }
    }
    throw new IllegalStateException("PORTABLE_DEVELOPER_DOWNLOADS escaped the portable root.");
  }

  private static boolean parseBooleanEnvironment(String name) {
    String value = requiredEnvironment(name);
    if (value.equalsIgnoreCase("true")) return true;
    if (value.equalsIgnoreCase("false")) return false;
    throw new IllegalStateException(name + " must be true or false.");
  }

  private static void verifyManifest(Path manifest, Path master) throws IOException {
    List<String> lines = Files.readAllLines(manifest, StandardCharsets.UTF_8);
    HashSet<Path> expected = new HashSet<>();
    long totalBytes = 0;
    int fileCount = 0;
    for (String line : lines) {
      if (!line.startsWith("file=")) continue;
      String[] parts = line.substring(5).split("\\|", -1);
      if (parts.length != 3) throw new IOException("The profile manifest contains an invalid entry.");
      String relativeText;
      long expectedSize;
      try {
        relativeText = new String(Base64.getDecoder().decode(parts[0]), StandardCharsets.UTF_8);
        expectedSize = Long.parseLong(parts[1]);
      } catch (IllegalArgumentException exception) {
        throw new IOException("The profile manifest contains invalid encoded data.", exception);
      }
      Path file = master.resolve(relativeText).normalize();
      if (!file.startsWith(master)
          || !Files.isRegularFile(file, LinkOption.NOFOLLOW_LINKS)
          || Files.isSymbolicLink(file)
          || Files.size(file) != expectedSize
          || !sha256(file).equalsIgnoreCase(parts[2])
          || !expected.add(file)) {
        throw new IOException("A master profile file does not match its manifest.");
      }
      totalBytes += expectedSize;
      fileCount++;
      if (fileCount > 25000 || totalBytes > 2147483648L) {
        throw new IOException("The profile exceeds portable safety limits.");
      }
    }
    try (var files = Files.walk(master)) {
      long actualCount = files.filter(path -> Files.isRegularFile(path, LinkOption.NOFOLLOW_LINKS)).count();
      if (actualCount != fileCount) {
        throw new IOException("The master profile contains files not covered by its manifest.");
      }
    }
  }

  private static String sha256(Path path) throws IOException {
    try {
      MessageDigest digest = MessageDigest.getInstance("SHA-256");
      try (InputStream input = Files.newInputStream(path)) {
        byte[] buffer = new byte[81920];
        int read;
        while ((read = input.read(buffer)) >= 0) {
          if (read > 0) digest.update(buffer, 0, read);
        }
      }
      return HexFormat.of().formatHex(digest.digest());
    } catch (NoSuchAlgorithmException exception) {
      throw new IOException("SHA-256 is not available.", exception);
    }
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

  private record ProfileDefinition(String browser, Path master, String profileDirectory) {}
}

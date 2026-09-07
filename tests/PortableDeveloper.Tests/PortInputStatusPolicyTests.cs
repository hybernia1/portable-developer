using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class PortInputStatusPolicyTests
{
    private readonly UiText _text = new(new InMemorySettingsStore());

    [Fact]
    public void Invalid_port_wins_without_probing_the_host()
    {
        var probes = 0;

        var status = Resolve(79, [79, 8080], isAvailable: _ =>
        {
            probes++;
            return true;
        });

        Assert.Equal(_text.PortInvalid, status);
        Assert.Equal(0, probes);
    }

    [Fact]
    public void Duplicate_port_wins_without_probing_the_host()
    {
        var probes = 0;

        var status = Resolve(8080, [8080, 8080], isAvailable: _ =>
        {
            probes++;
            return true;
        });

        Assert.Equal(_text.PortDuplicate, status);
        Assert.Equal(0, probes);
    }

    [Fact]
    public void Current_owned_port_is_reported_as_application_owned()
    {
        var status = Resolve(8080, [8080, 9000], currentPort: 8080, owned: true);

        Assert.Equal(_text.PortUsedByApplication, status);
    }

    [Fact]
    public void Listener_and_bind_probe_distinguish_occupied_and_available_ports()
    {
        var listener = new TcpPortListenerInfo("127.0.0.1", 8080);

        Assert.Equal(_text.PortOccupied, Resolve(8080, [8080, 9000], listeners: [listener]));
        Assert.Equal(_text.PortOccupied, Resolve(9000, [8080, 9000], isAvailable: _ => false));
        Assert.Equal(_text.PortAvailable, Resolve(9000, [8080, 9000]));
    }

    private string Resolve(
        int port,
        IReadOnlyCollection<int> proposed,
        int currentPort = 8081,
        bool owned = false,
        IReadOnlyCollection<TcpPortListenerInfo>? listeners = null,
        Func<int, bool>? isAvailable = null) =>
        PortInputStatusPolicy.Resolve(
            _text,
            port,
            proposed,
            currentPort,
            owned,
            listeners ?? [],
            isAvailable ?? (_ => true));

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}

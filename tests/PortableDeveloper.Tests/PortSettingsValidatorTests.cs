using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.Tests;

public sealed class PortSettingsValidatorTests
{
    [Fact]
    public void Validate_accepts_four_unique_user_ports()
    {
        var settings = new PortSettings(8081, 9001, 3308, 4445);

        Assert.Same(settings, PortSettingsValidator.Validate(settings));
    }

    [Theory]
    [InlineData(1023)]
    [InlineData(65536)]
    public void Validate_rejects_port_outside_user_range(int port)
    {
        var settings = PortSettings.Default with { ApachePort = port };

        Assert.Throws<ArgumentOutOfRangeException>(() => PortSettingsValidator.Validate(settings));
    }

    [Fact]
    public void Validate_rejects_duplicate_service_ports()
    {
        var settings = PortSettings.Default with { SeleniumPort = PortSettings.Default.ApachePort };

        Assert.Throws<ArgumentException>(() => PortSettingsValidator.Validate(settings));
    }
}

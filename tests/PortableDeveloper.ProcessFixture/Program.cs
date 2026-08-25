using System.Text;
using System.Diagnostics;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

if (args is ["--no-input"])
{
    Console.WriteLine("Příkaz bez standardního vstupu proběhl správně.");
    return;
}

if (args is ["--wait"])
{
    Console.Write("READY");
    Console.Out.Flush();
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

if (args is ["--child-wait"])
{
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

if (args is ["--spawn-child"])
{
    using var child = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--child-wait")
    {
        UseShellExecute = false,
        CreateNoWindow = true
    }) ?? throw new InvalidOperationException("The child fixture did not start.");
    Console.WriteLine($"PARENT READY {child.Id}");
    Console.Out.Flush();
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

Console.Write("CZ: ");
Console.Out.Flush();
var input = Console.ReadLine() ?? string.Empty;
Console.WriteLine($"Překlad: Dobrý den — {input}");

using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

if (args is ["--wait"])
{
    Console.Write("READY");
    Console.Out.Flush();
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

Console.Write("CZ: ");
Console.Out.Flush();
var input = Console.ReadLine() ?? string.Empty;
Console.WriteLine($"Překlad: Dobrý den — {input}");

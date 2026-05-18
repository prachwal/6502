using Cpu6502.System.Apple1;
using Cpu6502.System.Terminal;
using System.Collections.Concurrent;
using System.Globalization;

var runOptions = ParseArgs(args);
Console.Out.WriteLine(runOptions.UseBasic ? "Apple-1 WOZ Monitor + Integer BASIC ROM" : "Apple-1 WOZ Monitor");
Console.Out.WriteLine("Examples: 0.3<Enter> examine memory, 300R run at address $0300");
if (runOptions.UseBasic)
    Console.Out.WriteLine("BASIC profile loaded. From WOZ Monitor, use E000R to start Integer BASIC.");
Console.Out.WriteLine("Esc clears the current line, Backspace deletes one character, Ctrl+C exits");
Console.Out.WriteLine();
Console.Out.Flush();

var terminal = new BufferedTerminalLink();
var host = new Apple1Host(terminal, runOptions.ToApple1Options());
host.Reset();

Console.Out.WriteLine("Monitor ready. Waiting for input...");
Console.Out.Flush();

var pendingInput = new ConcurrentQueue<byte>();
StartConsoleInputReader(pendingInput);

try
{
    while (true)
    {
        while (pendingInput.TryDequeue(out byte inputByte))
            host.TypeKey(inputByte);
        
        host.Step();

        string output = host.ReadOutput();
        if (output.Length == 0)
            continue;

        Console.Out.Write(output);
        Console.Out.Flush();
    }
}
catch (Exception ex)
{
    Console.Out.WriteLine($"\nError: {ex.Message}");
    Console.Out.WriteLine(ex.StackTrace);
    Console.Out.Flush();
}

static byte ToApple1InputByte(char keyChar)
{
    return Apple1KeyMapper.MapCharacter(keyChar);
}

static byte ToApple1InputByteFromKey(ConsoleKeyInfo keyInfo)
{
    return keyInfo.Key switch
    {
        ConsoleKey.Enter when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Enter, out byte value) => value,
        ConsoleKey.Escape when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Escape, out byte value) => value,
        ConsoleKey.Backspace when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Backspace, out byte value) => value,
        ConsoleKey.Delete when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Delete, out byte value) => value,
        ConsoleKey.LeftArrow when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.LeftArrow, out byte value) => value,
        _ => ToApple1InputByte(keyInfo.KeyChar)
    };
}

static void StartConsoleInputReader(ConcurrentQueue<byte> pendingInput)
{
    var inputThread = new Thread(() =>
    {
        try
        {
            while (true)
            {
                if (Console.IsInputRedirected)
                {
                    int input = Console.In.Read();
                    if (input < 0)
                        return;

                    pendingInput.Enqueue(ToApple1InputByte((char)input));
                }
                else
                {
                    pendingInput.Enqueue(ToApple1InputByteFromKey(Console.ReadKey(intercept: true)));
                }
            }
        }
        catch
        {
            // Console input can disappear when the host closes stdin.
        }
    })
    {
        IsBackground = true,
        Name = "Apple-1 console input"
    };

    inputThread.Start();
}

static RunOptions ParseArgs(string[] args)
{
    var options = new RunOptions();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (arg == "--basic")
        {
            options.UseBasic = true;
            options.ProfilePath = Apple1Options.BasicProfilePath;
        }
        else if (arg == "--profile" && i + 1 < args.Length)
        {
            options.ProfilePath = args[++i];
        }
        else if (arg == "--entry" && i + 1 < args.Length)
        {
            options.EntryPoint = ParseHexUshort(args[++i]);
        }
    }

    return options;
}

static ushort ParseHexUshort(string value)
{
    string text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
    return ushort.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}

sealed class RunOptions
{
    public bool UseBasic { get; set; }
    public string ProfilePath { get; set; } = Apple1Options.WozMonitorProfilePath;
    public ushort EntryPoint { get; set; } = 0xFF00;

    public Apple1Options ToApple1Options() => new(
        ProfilePath: ProfilePath,
        EntryPoint: EntryPoint);
}

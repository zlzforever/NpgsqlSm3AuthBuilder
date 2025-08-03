// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using NpgsqlSm3AuthBuilder;

var originalColor = Console.ForegroundColor;

Console.WriteLine("Welcome to Npgsql SM3 Authentication Builder!");
Console.WriteLine();

var arguments = new CommandLineParser(args);

var npgsqlVersion = arguments.GetValue("version");
if (string.IsNullOrWhiteSpace(npgsqlVersion))
{
    PrintLine("ERROR: Npgsql version is required, like v9.0.3", ConsoleColor.Red);
    return;
}

var npgsqlBase = arguments.GetValue("source");
if (string.IsNullOrWhiteSpace(npgsqlBase))
{
    PrintLine("ERROR: Npgsql source code folder is required", ConsoleColor.Red);
    return;
}

var output = arguments.GetValue("output");
if (string.IsNullOrWhiteSpace(output))
{
    PrintLine("ERROR: output directory is required", ConsoleColor.Red);
    return;
}

if (!Directory.Exists(output))
{
    Directory.CreateDirectory(output);
}

var requestTypePath = Path.Combine(npgsqlBase, "src/Npgsql/BackendMessages/AuthenticationRequestType.cs");
var sm3PasswordMessagePath = Path.Combine(npgsqlBase, "src/Npgsql/Internal/AuthenticationSm3PasswordMessage.cs");
var sm3Path = Path.Combine(npgsqlBase, "src/Npgsql/Internal/Sm3.cs");
var connectorSm3Path = Path.Combine(npgsqlBase, "src/Npgsql/Internal/NpgsqlConnector.Sm3.cs");
var requireAuthModePath = Path.Combine(npgsqlBase, "src/Npgsql/RequireAuthMode.cs");
var assembly = typeof(Program).Assembly;

var step = 0;
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Cleaning old files...");
Clean();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Add SM3 authentication request type");
HandleAuthenticationRequestType();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Add SM3 authentication password message");
HandleAuthenticationSm3PasswordMessage();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Add HighGo sm3 hash algorithm");
HandleSm3();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Add AuthenticateSm3 method to NpgsqlConnector");
HandleSm3Connector();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Support load AuthenticationSm3PasswordMessage from server buffer in NpgsqlConnector.Auth.cs");
HandleParseServerMessage();
// Console.WriteLine($"=============== Step {++step} ===============");
// Console.WriteLine("Add RequiredAuthMode");
// HandleRequiredAuthMode();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Call AuthenticateSm3 when server use SM3 in NpgsqlConnector.Auth.cs");
HandleAuth();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Build Npgsql with SM3 support");
Build();
Console.WriteLine($"=============== Step {++step} ===============");
Console.WriteLine("Build completed, cleaning up old files...");
Clean();
Console.WriteLine("Bye!");
return;

void PrintLine(string msg, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(msg);
    Console.ForegroundColor = originalColor;
}

void Build()
{
    var command = $"""
                   cd {npgsqlBase} && dotnet build src/Npgsql -c Release && mv -f src/Npgsql/bin/Release/net8.0/Npgsql.* {output}
                   """;

    var process = Process.Start("sh", $"-c \"{command}\"");
// 异步读取输出，避免死锁
    process.OutputDataReceived += (_, e) => { Console.WriteLine(e.Data); };
    process.ErrorDataReceived += (_, e) => { Console.WriteLine(e.Data); };
// 等待进程完成
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        PrintLine("Build failed, please check the output above.", ConsoleColor.Red);
    }
}

void Clean()
{
    var files = new[]
    {
        requestTypePath,
        sm3PasswordMessagePath,
        sm3Path,
        connectorSm3Path,
        requireAuthModePath
    };
    foreach (var file in files)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    var cleanCommand = $"""
                        cd {npgsqlBase} && git checkout . && git checkout v9.0.3
                        """;

    var process = Process.Start("sh", $"-c \"{cleanCommand}\"");
    process.OutputDataReceived += (_, e) => { Console.WriteLine(e.Data); };
    process.ErrorDataReceived += (_, e) => { Console.WriteLine(e.Data); };
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        PrintLine("Clean failed, please check the output above.", ConsoleColor.Red);
    }
}

void HandleAuth()
{
    var path = Path.Combine(npgsqlBase, "src/Npgsql/Internal/NpgsqlConnector.Auth.cs");
    var codes = File.ReadAllText(path);
    if (string.IsNullOrEmpty(codes))
    {
        throw new ApplicationException("NpgsqlConnector.Auth.cs file is empty or not found.");
    }

    if (codes.Contains("case AuthenticationRequestType.Sm3Password"))
    {
        return;
    }

    codes = codes.Replace(
        "default:\n                throw new NotSupportedException($\"Authentication method not supported (Received: {msg.AuthRequestType})\");",
        """
        case AuthenticationRequestType.Sm3Password:
            await AuthenticateSm3(username, ((AuthenticationSm3PasswordMessage)msg).Salt, async, cancellationToken).ConfigureAwait(false);
            break;
        default:
            throw new NotSupportedException($"Authentication method not supported (Received: {msg.AuthRequestType})");
        """
    );
    File.WriteAllText(path, codes);
}

// void HandleRequiredAuthMode()
// {
//     var path = Path.Combine(npgsqlBase, "src/Npgsql/NpgsqlConnectionStringBuilder.cs");
//     var authenticationRequestType =
//         "enum\\s+RequireAuthMode\\s*\\{\\s*([A-Za-z0-9]+\\s*=\\s*\\d+,\\s*)*([A-Za-z0-9]+\\s*=\\s*\\d+)\\s*\\}";
//
//     var text = File.ReadAllText(path);
//     text = Regex.Replace(text, authenticationRequestType, _ => "");
//     File.WriteAllText(path, text);
//
//     var codes = GetText("RequireAuthMode.txt");
//     File.WriteAllText(Path.Combine(npgsqlBase, "src/Npgsql/RequireAuthMode.cs"), codes);
// }

void HandleParseServerMessage()
{
    var path = Path.Combine(npgsqlBase, "src/Npgsql/Internal/NpgsqlConnector.cs");
    var codes = File.ReadAllText(path);
    if (string.IsNullOrEmpty(codes))
    {
        throw new ApplicationException("NpgsqlConnector.cs file is empty or not found.");
    }

    if (codes.Contains("AuthenticationRequestType.Sm3Password => AuthenticationSm3PasswordMessage.Load(buf),"))
    {
        return;
    }

    codes = codes.Replace(
        "_ => throw new NotSupportedException($\"Authentication method not supported (Received: {authType})\")",
        """
        AuthenticationRequestType.Sm3Password => AuthenticationSm3PasswordMessage.Load(buf),
                        _ => throw new NotSupportedException($"Authentication method not supported (Received: {authType})")
        """
    );
    File.WriteAllText(path, codes);
}

void HandleSm3Connector()
{
    var codes = GetText("NpgsqlConnectorSm3.txt");
    File.WriteAllText(connectorSm3Path, codes);
}

void HandleSm3()
{
    var codes = GetText("Sm3.txt");
    File.WriteAllText(sm3Path, codes);
}

void HandleAuthenticationSm3PasswordMessage()
{
    var codes = GetText("AuthenticationSm3PasswordMessage.txt");
    File.WriteAllText(sm3PasswordMessagePath, codes);
}

void HandleAuthenticationRequestType()
{
    var path = Path.Combine(npgsqlBase, "src/Npgsql/BackendMessages/AuthenticationMessages.cs");
    var authenticationRequestType =
        "enum\\s+AuthenticationRequestType\\s*\\{\\s*([A-Za-z0-9]+\\s*=\\s*\\d+,\\s*)*([A-Za-z0-9]+\\s*=\\s*\\d+)\\s*\\}";

    var text = File.ReadAllText(path);
    text = Regex.Replace(text, authenticationRequestType,
        _ => "");
    // 删除原文件中的 AuthenticationRequestType 枚举
    File.WriteAllText(path, text);
    // 添加新文件， 新文件包含 SM3 类型
    var codes = GetText("AuthenticationRequestType.txt");
    File.WriteAllText(requestTypePath, codes);
}

string GetText(string name)
{
    using var stream = assembly.GetManifestResourceStream("NpgsqlSm3AuthBuilder." + name);
    if (stream == null)
    {
        PrintLine($"Source code {name} is missing", ConsoleColor.Red);
        Environment.Exit(-1);
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
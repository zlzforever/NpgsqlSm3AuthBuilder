namespace NpgsqlSm3AuthBuilder;

public class CommandLineParser
{
    private readonly Dictionary<string, string> _parameters = new();

    public CommandLineParser(string[] args)
    {
        Parse(args);
    }

    private void Parse(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            // 检查是否是参数键（以 -- 开头）
            if (args[i].StartsWith("--"))
            {
                string key = args[i].Substring(2); // 去除 -- 前缀

                // 检查是否有对应的值
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    _parameters[key] = args[i + 1];
                    i++; // 跳过已处理的值
                }
                else
                {
                    // 如果没有值，设置为空字符串或标记
                    _parameters[key] = string.Empty;
                }
            }
        }
    }

    // 获取参数值，如果不存在则返回默认值
    public string GetValue(string key, string defaultValue = "")
    {
        return _parameters.GetValueOrDefault(key, defaultValue);
    }

    // 检查是否包含指定参数
    public bool ContainsKey(string key)
    {
        return _parameters.ContainsKey(key);
    }

    // 获取所有参数
    public IReadOnlyDictionary<string, string> GetAllParameters()
    {
        return new Dictionary<string, string>(_parameters);
    }
}
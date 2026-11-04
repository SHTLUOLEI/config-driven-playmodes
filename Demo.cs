using System;
using System.Text.Json;

namespace ConfigDrivenPlaymodes
{
    /// <summary>示例玩法：点击反应小游戏。注意它完全不知道演出、存档、暂停的存在。</summary>
    internal sealed class TapReflexGame : IPlayMode
    {
        private PlayContext _context;
        private int _targetTaps;
        private int _taps;

        public void Prepare(PlayContext context, JsonElement config)
        {
            _context = context;
            _targetTaps = config.TryGetProperty("targetTaps", out var v) ? v.GetInt32() : 5;
            Console.WriteLine($"[game] 准备完成，目标连击 {_targetTaps} 次");
        }

        public void Start()
        {
            Console.WriteLine("[game] 开始，模拟玩家点击……");
            for (var i = 0; i < _targetTaps; i++) _taps++;
            Console.WriteLine($"[game] 达成 {_taps}/{_targetTaps}，上报结算");
            _context.Report(PlayResult.Completed);   // 只上报结果，不写存档
        }

        public void SetPaused(bool paused) => Console.WriteLine($"[game] 暂停 = {paused}");
        public void Stop() => Console.WriteLine("[game] 停止");
    }

    internal static class Demo
    {
        private static void Main()
        {
            var runtime = new PlayModeRuntime();
            runtime.Register("tap-reflex", () => new TapReflexGame());

            // 差异全部来自配置；新增一个玩法 = 注册一行 + 配置表加一行。
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<PlaySpec>("""
            {
                "id": "tap-reflex",
                "introBeats": ["猫咪打了个哈欠", "聚光灯亮起"],
                "outroBeats": ["撒花", "返回卧室"],
                "config": { "targetTaps": 3 }
            }
            """, options);

            var context = new PlayContext("session-001",
                result => Console.WriteLine($"[runtime] 收到结算：{result}"));

            runtime.Begin(spec, context);
            runtime.End(spec);
        }
    }
}

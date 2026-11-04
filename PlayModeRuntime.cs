using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ConfigDrivenPlaymodes
{
    /// <summary>
    /// 配置驱动的多玩法运行层（简化版）。
    ///
    /// 设计动机：当项目里有 10+ 个小玩法时，为每个玩法写一套
    /// 「加载 → 开场演出 → 游玩 → 结算 → 收尾演出 → 释放」的状态机，
    /// 会产生大量重复代码，且行为难以统一（暂停/重试/断点恢复各写各的）。
    ///
    /// 解法：把「玩法差异」收敛为一张配置表 + 一个最小契约，
    /// 运行层只写一次，玩法只实现 4 个方法。
    /// </summary>

    /// <summary>玩法契约：玩法作者只需要实现这 4 个方法。</summary>
    public interface IPlayMode
    {
        void Prepare(PlayContext context, JsonElement config);
        void Start();
        void SetPaused(bool paused);
        void Stop();
    }

    /// <summary>运行层提供给玩法的回传通道（结算、检查点），玩法不直接碰存档。</summary>
    public sealed class PlayContext
    {
        public string SessionId { get; }
        private readonly Action<PlayResult> _report;
        public PlayContext(string sessionId, Action<PlayResult> report)
        { SessionId = sessionId; _report = report; }
        public void Report(PlayResult result) => _report(result);
    }

    public enum PlayResult { Completed, Failed, Aborted }

    /// <summary>配置表条目：玩法的「身份证 + 差异参数」。</summary>
    public sealed class PlaySpec
    {
        public string Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string[] IntroBeats { get; set; } = Array.Empty<string>();  // 开场演出，秒级节奏由运行层统一推进
        public string[] OutroBeats { get; set; } = Array.Empty<string>();
        public JsonElement Config { get; set; }                          // 玩法私有参数，运行层不解读
    }

    /// <summary>运行层：统一的生命周期状态机，与具体玩法解耦。</summary>
    public sealed class PlayModeRuntime
    {
        private readonly Dictionary<string, Func<IPlayMode>> _factories = new();
        private IPlayMode _current;

        public void Register(string id, Func<IPlayMode> factory) => _factories[id] = factory;

        public void Begin(PlaySpec spec, PlayContext context)
        {
            if (!spec.Enabled) throw new InvalidOperationException($"玩法 {spec.Id} 未启用");
            if (!_factories.TryGetValue(spec.Id, out var factory))
                throw new InvalidOperationException($"玩法 {spec.Id} 未注册");
            _current = factory();
            _current.Prepare(context, spec.Config);   // 先在遮罩下备齐首帧
            foreach (var beat in spec.IntroBeats) Console.WriteLine($"[intro] {beat}");
            _current.Start();                          // 演出结束才放行操作
        }

        public void End(PlaySpec spec)
        {
            _current?.Stop();
            foreach (var beat in spec.OutroBeats) Console.WriteLine($"[outro] {beat}");
            _current = null;
        }
    }
}

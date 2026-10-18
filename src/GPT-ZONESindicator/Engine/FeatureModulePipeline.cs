using System;
using System.Collections.Generic;

namespace cAlgo
{
    /// <summary>
    /// Small orchestration boundary for an independently switchable feature.
    /// Feature algorithms remain in their own source files; this dispatcher isolates
    /// failures so one broken feature does not prevent unrelated ones from updating.
    /// </summary>
    internal sealed class FeatureModulePipeline
    {
        private sealed class ProcessModule
        {
            public string Name;
            public Func<bool> Enabled;
            public Action<int, double> Execute;
        }

        private sealed class RenderModule
        {
            public string Name;
            public Func<bool> Enabled;
            public Action<bool> Execute;
        }

        private readonly List<ProcessModule> _process = new List<ProcessModule>(8);
        private readonly List<RenderModule> _render = new List<RenderModule>(8);
        private readonly Action<string, Exception> _onError;

        public FeatureModulePipeline(Action<string, Exception> onError)
        {
            _onError = onError;
        }

        public void AddProcessor(string name, Func<bool> enabled, Action<int, double> execute)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A feature name is required.", nameof(name));
            if (enabled == null)
                throw new ArgumentNullException(nameof(enabled));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _process.Add(new ProcessModule { Name = name, Enabled = enabled, Execute = execute });
        }

        public void AddRenderer(string name, Func<bool> enabled, Action<bool> execute)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A feature name is required.", nameof(name));
            if (enabled == null)
                throw new ArgumentNullException(nameof(enabled));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _render.Add(new RenderModule { Name = name, Enabled = enabled, Execute = execute });
        }

        public void ProcessBar(int index, double atr)
        {
            for (int i = 0; i < _process.Count; i++)
            {
                ProcessModule module = _process[i];
                try
                {
                    if (module.Enabled())
                        module.Execute(index, atr);
                }
                catch (Exception ex)
                {
                    if (_onError != null)
                        _onError(module.Name, ex);
                }
            }
        }

        public void Render(bool refreshMetadata)
        {
            for (int i = 0; i < _render.Count; i++)
            {
                RenderModule module = _render[i];
                try
                {
                    if (module.Enabled())
                        module.Execute(refreshMetadata);
                }
                catch (Exception ex)
                {
                    if (_onError != null)
                        _onError(module.Name, ex);
                }
            }
        }
    }
}

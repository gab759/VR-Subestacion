using System.Collections.Generic;
using UnityEngine;

namespace VRSubestacion.EditorTools
{
    public enum WiringSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct WiringIssue
    {
        public readonly WiringSeverity Severity;
        public readonly string Message;
        public readonly Object Context;

        public WiringIssue(WiringSeverity severity, string message, Object context)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }
    }

    public sealed class WiringReport
    {
        private readonly List<WiringIssue> _issues = new List<WiringIssue>();

        public IReadOnlyList<WiringIssue> Issues => _issues;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public void Add(WiringSeverity severity, string message, Object context = null)
        {
            _issues.Add(new WiringIssue(severity, message, context));
            if (severity == WiringSeverity.Error)
                ErrorCount++;
            else if (severity == WiringSeverity.Warning)
                WarningCount++;
        }

        public void Error(string message, Object context = null) => Add(WiringSeverity.Error, message, context);
        public void Warning(string message, Object context = null) => Add(WiringSeverity.Warning, message, context);
        public void Info(string message, Object context = null) => Add(WiringSeverity.Info, message, context);

        public bool Contains(WiringSeverity severity, string fragment)
        {
            for (int i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Severity == severity && _issues[i].Message.Contains(fragment))
                    return true;
            }
            return false;
        }

        public void LogToConsole(string header)
        {
            for (int i = 0; i < _issues.Count; i++)
            {
                WiringIssue issue = _issues[i];
                string line = "[" + header + "] " + issue.Message;
                switch (issue.Severity)
                {
                    case WiringSeverity.Error:
                        Debug.LogError(line, issue.Context);
                        break;
                    case WiringSeverity.Warning:
                        Debug.LogWarning(line, issue.Context);
                        break;
                    default:
                        Debug.Log(line, issue.Context);
                        break;
                }
            }

            Debug.Log($"[{header}] Resultado: {ErrorCount} error(es), {WarningCount} advertencia(s).");
        }
    }
}

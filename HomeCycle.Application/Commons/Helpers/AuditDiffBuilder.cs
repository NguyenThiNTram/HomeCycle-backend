using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public sealed class AuditDiffBuilder
    {
        private readonly Dictionary<string, object?> _oldValues = new();
        private readonly Dictionary<string, object?> _newValues = new();

        public IReadOnlyDictionary<string, object?>? OldValues => _oldValues.Count == 0 ? null : _oldValues;
        public IReadOnlyDictionary<string, object?>? NewValues => _newValues.Count == 0 ? null : _newValues;

        public AuditDiffBuilder Add<T>(string fieldName, T oldValue, T newValue)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return this;

            _oldValues[fieldName] = oldValue;
            _newValues[fieldName] = newValue;
            return this;
        }
    }
}

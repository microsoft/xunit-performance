// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Xunit.Performance.Api.Profilers.Etw
{
    /// <summary>
    /// Defines a performance monitor counter object as collected by the Event Tracing for Windows.
    /// </summary>
    public sealed class PerformanceMonitorCounter : IEquatable<PerformanceMonitorCounter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitorCounter"/> class.
        /// </summary>
        /// <param name="displayName">User friendly name given to the performance monitor counter.</param>
        /// <param name="name">Performance monitor counter name.</param>
        /// <param name="unit">Performance monitor counter unit.</param>
        /// <param name="id">Performance monitor counter Id.</param>
        public PerformanceMonitorCounter(string displayName, string name, string unit, int id)
        {
            DisplayName = displayName;
            Name = name;
            Unit = unit;
            Id = id;
        }

        /// <summary>
        /// Performance monitor counter friendly name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Performance monitor counter Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Performance monitor counter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Performance monitor counter unit.
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Determines whether two specified <see cref="PerformanceMonitorCounter"/> objects have different values.
        /// </summary>
        /// <param name="lhs">The first <see cref="PerformanceMonitorCounter"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="PerformanceMonitorCounter"/> to compare, or null.</param>
        /// <returns>True if the value of a is different from the value of b; otherwise, false.</returns>
        public static bool operator !=(PerformanceMonitorCounter lhs, PerformanceMonitorCounter rhs) => !(lhs == rhs);

        /// <summary>
        /// Determines whether two specified <see cref="PerformanceMonitorCounter"/> objects have the same value.
        /// </summary>
        /// <param name="lhs">The first <see cref="PerformanceMonitorCounter"/> to compare, or null.</param>
        /// <param name="rhs">The second <see cref="PerformanceMonitorCounter"/> to compare, or null.</param>
        /// <returns>True if its two operands refer to the same object or if the values of its operands are equal; otherwise, false.</returns>
        public static bool operator ==(PerformanceMonitorCounter lhs, PerformanceMonitorCounter rhs) => (object)lhs != null ? lhs.Equals(rhs) : (object)rhs == null;

        /// <summary>
        /// Determines whether the specified object is equals to this object.
        /// </summary>
        /// <param name="obj">The object to compare with this object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(obj as PerformanceMonitorCounter);

        /// <summary>
        /// Indicates whether the current object is equal to another <see cref="PerformanceMonitorCounter"/> object.
        /// </summary>
        /// <param name="other">A <see cref="PerformanceMonitorCounter"/> object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(PerformanceMonitorCounter other)
        {
            if ((object)other == null)
                return false;
            return Id == other.Id
                && DisplayName == other.DisplayName
                && Name == other.Name
                && Unit == other.Unit;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => Id ^ DisplayName.GetHashCode() ^ Name.GetHashCode() ^ Unit.GetHashCode();
    }
}
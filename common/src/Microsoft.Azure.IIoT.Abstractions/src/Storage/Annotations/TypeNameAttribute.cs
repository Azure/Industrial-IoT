
namespace Microsoft.Azure.IIoT.Storage.Annotations {
    using System;

    /// <summary>
    /// Declarative type name
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    public sealed class TypeNameAttribute : Attribute {

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="TypeNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public TypeNameAttribute(string name) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Dynamic.Castle {
    using global::Castle.DynamicProxy;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Cache proxy for storage
    /// </summary>
    public class ProxyFactory : IInterceptorSelector, IProxyFactory {

        /// <summary>
        /// Create default cache proxy
        /// </summary>
        public ProxyFactory() {
            _proxyGenerator = new ProxyGenerator();
            _hook = new SelectorScopedGenerationHook(this);
            _interceptors = new List<IProxyInterceptor>();
        }

        /// <summary>
        /// Add inteceptor
        /// </summary>
        /// <param name="interceptor"></param>
        public void AddInterceptor(IProxyInterceptor interceptor) =>
            _interceptors.Add(interceptor);

        /// <summary>
        /// Remove inteceptor
        /// </summary>
        /// <param name="interceptor"></param>
        public void RemoveInterceptor(IProxyInterceptor interceptor) =>
            _interceptors.Remove(interceptor);

        /// <summary>
        /// Select interceptors that can intercept the method/type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method,
            IInterceptor[] interceptors) => interceptors.OfType<IProxyInterceptor>()
                .Where(i => i.CanIntercept(type, method))
                .OfType<IInterceptor>()
                .ToArray();

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public T CreateProxyT<T>(T target, object mixin) where T : class =>
            _proxyGenerator.CreateInterfaceProxyWithTarget(target, GetOptions(mixin),
                _interceptors.OfType<IInterceptor>().ToArray());

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public object CreateProxy(object target, Type type, object mixin) =>
            _proxyGenerator.CreateInterfaceProxyWithTarget(type, target, GetOptions(mixin),
                _interceptors.OfType<IInterceptor>().ToArray());

        /// <summary>
        /// Create proxy as continuation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public Task<T> CreateProxyAsContinuationT<T>(Task<T> target, object mixin)
            where T : class => target.ContinueWith(t =>
            _proxyGenerator.CreateInterfaceProxyWithTarget(t.Result, GetOptions(mixin),
                _interceptors.OfType<IInterceptor>().ToArray()));

        /// <summary>
        /// Create proxy for target interface using reflection on
        /// generic call
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public object CreateProxyAsContinuation(Task target, Type type, object mixin) =>
            _createProxyAsContinuationT.MakeGenericMethod(type).Invoke(
                this, new[] { target, mixin });

        /// <summary>
        /// Helper to make options
        /// </summary>
        /// <param name="mixin"></param>
        /// <returns></returns>
        private ProxyGenerationOptions GetOptions(object mixin) {
            var options = new ProxyGenerationOptions {
                Selector = this,
                Hook = _hook
            };
            if (mixin != null) {
                options.AddMixinInstance(mixin);
            }
            return options;
        }

        /// <summary>
        /// Internal proxy generation hook to hook the right functionality.
        /// </summary>
        private class SelectorScopedGenerationHook : AllMethodsHook {
            public SelectorScopedGenerationHook(ProxyFactory factory) {
                _factory = factory;
            }

            /// <summary>
            /// Select whether to intercept or call function directly
            /// </summary>
            /// <param name="type"></param>
            /// <param name="methodInfo"></param>
            /// <returns></returns>
            public override bool ShouldInterceptMethod(Type type, MethodInfo methodInfo) =>
                methodInfo.GetBaseDefinition().DeclaringType != typeof(object) &&
                base.ShouldInterceptMethod(type, methodInfo) &&
                _factory._interceptors.Any(i => i.CanIntercept(type, methodInfo));

            private readonly ProxyFactory _factory;
        }

        private static readonly MethodInfo _createProxyAsContinuationT =
            typeof(ProxyFactory).GetMethod(nameof(CreateProxyAsContinuationT),
                BindingFlags.Public | BindingFlags.Instance);

        private readonly IProxyGenerator _proxyGenerator;
        private readonly SelectorScopedGenerationHook _hook;
        private readonly List<IProxyInterceptor> _interceptors;
    }
}

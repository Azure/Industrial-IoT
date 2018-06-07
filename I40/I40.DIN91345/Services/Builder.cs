// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace i40.DIN91345.Builder {
    using I40.DIN91345.Models;
    using I40.Common.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using I40.DINPVSxx.Models;

    public interface IBuilder {

        /// <summary>
        /// Add submodel
        /// </summary>
        /// <param name="func"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        IPackageBuilder AddSubmodel(Func<ISubmodelBuilder, Submodel> func,
            string category);
    }

    public interface IPackageBuilder : IBuilder {

        /// <summary>
        /// Add packageable item to package
        /// </summary>
        /// <param name="item"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        IPackageBuilder AddPackageable(IPackageable item, string category);

        Package Build();
    }

    public interface ISubmodelBuilder : IBuilder {

        /// <summary>
        /// Add property
        /// </summary>
        /// <param name="item"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        ISubmodelBuilder AddProperty(Property property);

        Submodel Build(Kind kind);
    }

    public static class PackageBuilderEx {

        /// <summary>
        /// Add to default category
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IPackageBuilder AddPackageable(this IPackageBuilder builder,
            IPackageable item) =>
            builder.AddPackageable(item, null);

        /// <summary>
        /// Add a submodel using submodel builder
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="func"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static IPackageBuilder AddSubmodel(this IPackageBuilder builder,
            Func<ISubmodelBuilder, Submodel> func) =>
            builder.AddPackageable(func(null), null);

    }
}

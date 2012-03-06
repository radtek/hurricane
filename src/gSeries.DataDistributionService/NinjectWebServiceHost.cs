// Copyright (c) 2012 XU, Jiang Yan <me@jxu.me>
//
// This software may be used and distributed according to the terms of the
// MIT license: http://www.opensource.org/licenses/mit-license.php

namespace GSeries.DataDistributionService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ServiceModel.Web;
    using System.ServiceModel.Description;
    using Ninject.Extensions.Wcf;

    public class NinjectWebServiceHost : WebServiceHost {
        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectServiceHost"/> class.
        /// </summary>
        public NinjectWebServiceHost() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectServiceHost"/> class.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        public NinjectWebServiceHost(TypeCode serviceType)
            : base(serviceType) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectServiceHost"/> class.
        /// </summary>
        /// <param name="singletonInstance">The singleton instance.</param>
        public NinjectWebServiceHost(object singletonInstance)
            : base(singletonInstance) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NinjectServiceHost"/> class.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="baseAddresses">The base addresses.</param>
        public NinjectWebServiceHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses) {
        }

        /// <summary>
        /// Invoked during the transition of a communication object into the opening state.
        /// </summary>
        protected override void OnOpening() {
            // FIXME: This oddly doesn't causes ArrayTypeMismatchException on Mono.
            Description.Behaviors.Add(new NinjectServiceBehavior());
            base.OnOpening();
        }
    }
}

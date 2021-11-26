using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.ObjectModel
{
    /// <summary>
    /// Provides information about the result of a method devirtualization process.
    /// </summary>
    public readonly struct MethodDevirtualizationResult 
    {
        /// <summary>
        /// Creates a new successful method devirtualization result. 
        /// </summary>
        /// <param name="method">The resolved method.</param>
        public MethodDevirtualizationResult(IMethodDescriptor method)
        {
            ResultingMethod = method ?? throw new ArgumentNullException(nameof(method));
            Exception = null;
            ResultingMethodSignature = null;
        }
        
        /// <summary>
        /// Creates a new successful method devirtualization result. 
        /// </summary>
        /// <param name="methodSig">The resolved Method Signature.</param>
        public MethodDevirtualizationResult(MethodSignature methodSig)
        {
            ResultingMethodSignature = methodSig ?? throw new ArgumentNullException(nameof(methodSig));
            Exception = null;
            ResultingMethod = null;
        }

        /// <summary>
        /// Creates a new unsuccessful method devirtualization result.
        /// </summary>
        /// <param name="exception">The exception that occurred during the method devirtualization.</param>
        public MethodDevirtualizationResult(Exception exception)
        {
            ResultingMethod = null;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            ResultingMethodSignature = null;
        }
        
        /// <summary>
        /// When successful, gets the resulting devirtualized method.
        /// </summary>
        public IMethodDescriptor ResultingMethod
        {
            get;
        }
        
        /// <summary>
        /// When successful, gets the resulting devirtualized method signature.
        /// </summary>
        public MethodSignature ResultingMethodSignature
        {
            get;
        }

        /// <summary>
        /// When unsuccessful, gets the exception thrown during the devirtualization process. 
        /// </summary>
        public Exception Exception
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the devirtualization process of the referenced method was successful.
        /// </summary>
        public bool IsSuccess => ResultingMethod != null || ResultingMethodSignature != null;

        /// <summary>
        /// Gets a value indicating whether the devirtualization process could not be completed due to an unknown
        /// object that was dereferenced.
        /// </summary>
        public bool IsUnknown => !IsSuccess && Exception is null;

        /// <summary>
        /// Gets the method signature of the method that was resolved.
        /// </summary>
        /// <returns>The signature.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Occurs when the dispatch was unsuccessful.</exception>
        public MethodSignature GetMethodSignature()
        {
            if (ResultingMethod != null)
                return ResultingMethod.Signature;
            if (ResultingMethodSignature != null)
                return ResultingMethodSignature;

            throw new ArgumentOutOfRangeException();
        }
    }
}
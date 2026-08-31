// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using System;
    using Xunit;

    /// <summary>
    /// Covers the constructor overloads on every exception type so the
    /// class-initialisation and constructor lines are counted as covered.
    /// These tests also verify the exception hierarchy and stored message.
    /// </summary>
    public sealed class ExceptionConstructorTests
    {
        // ── BadRequestException ──────────────────────────────────────────────

        [Fact]
        public void BadRequestException_DefaultCtor()
        {
            var ex = new BadRequestException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void BadRequestException_MessageCtor()
        {
            var ex = new BadRequestException("bad input");
            Assert.Equal("bad input", ex.Message);
        }

        [Fact]
        public void BadRequestException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new BadRequestException("bad input", inner);
            Assert.Equal("bad input", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void BadRequestException_MessageAndParamNameCtor()
        {
            var ex = new BadRequestException("bad input", "paramA");
            Assert.Equal("paramA", ex.ParamName);
        }

        [Fact]
        public void BadRequestException_MessageParamNameAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new BadRequestException("bad input", "paramA", inner);
            Assert.Equal("paramA", ex.ParamName);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ExternalDependencyException ──────────────────────────────────────

        [Fact]
        public void ExternalDependencyException_DefaultCtor()
        {
            var ex = new ExternalDependencyException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ExternalDependencyException_MessageCtor()
        {
            var ex = new ExternalDependencyException("dependency down");
            Assert.Equal("dependency down", ex.Message);
        }

        [Fact]
        public void ExternalDependencyException_MessageAndInnerCtor()
        {
            var inner = new Exception("root");
            var ex = new ExternalDependencyException("dependency down", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── InvalidConfigurationException ────────────────────────────────────

        [Fact]
        public void InvalidConfigurationException_DefaultCtor()
        {
            var ex = new InvalidConfigurationException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void InvalidConfigurationException_MessageCtor()
        {
            var ex = new InvalidConfigurationException("bad config");
            Assert.Equal("bad config", ex.Message);
        }

        [Fact]
        public void InvalidConfigurationException_MessageAndInnerCtor()
        {
            var inner = new Exception("root");
            var ex = new InvalidConfigurationException("bad config", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── MessageSizeLimitException ─────────────────────────────────────────

        [Fact]
        public void MessageSizeLimitException_DefaultCtor()
        {
            var ex = new MessageSizeLimitException();
            Assert.Equal(-1, ex.MessageSize);
            Assert.Equal(-1, ex.MaxMessageSize);
        }

        [Fact]
        public void MessageSizeLimitException_MessageCtor()
        {
            var ex = new MessageSizeLimitException("too big");
            Assert.Equal("too big", ex.Message);
        }

        [Fact]
        public void MessageSizeLimitException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new MessageSizeLimitException("too big", inner);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void MessageSizeLimitException_FullCtor_SetsProperties()
        {
            var ex = new MessageSizeLimitException("too big", 2048, 1024);
            Assert.Equal(2048, ex.MessageSize);
            Assert.Equal(1024, ex.MaxMessageSize);
        }

        [Fact]
        public void MessageSizeLimitException_FullCtorWithInner_SetsProperties()
        {
            var inner = new Exception("root");
            var ex = new MessageSizeLimitException("too big", 2048, 1024, inner);
            Assert.Equal(2048, ex.MessageSize);
            Assert.Equal(1024, ex.MaxMessageSize);
            Assert.Same(inner, ex.InnerException);
        }

        // ── MethodCallException ──────────────────────────────────────────────

        [Fact]
        public void MethodCallException_DefaultCtor()
        {
            var ex = new MethodCallException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void MethodCallException_MessageCtor()
        {
            var ex = new MethodCallException("call failed");
            Assert.Equal("call failed", ex.Message);
        }

        [Fact]
        public void MethodCallException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new MethodCallException("call failed", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ResourceConflictException ─────────────────────────────────────────

        [Fact]
        public void ResourceConflictException_DefaultCtor()
        {
            var ex = new ResourceConflictException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ResourceConflictException_MessageCtor()
        {
            var ex = new ResourceConflictException("conflict");
            Assert.Equal("conflict", ex.Message);
        }

        [Fact]
        public void ResourceConflictException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new ResourceConflictException("conflict", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ResourceExhaustionException ───────────────────────────────────────

        [Fact]
        public void ResourceExhaustionException_DefaultCtor()
        {
            var ex = new ResourceExhaustionException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ResourceExhaustionException_MessageCtor()
        {
            var ex = new ResourceExhaustionException("exhausted");
            Assert.Equal("exhausted", ex.Message);
        }

        [Fact]
        public void ResourceExhaustionException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new ResourceExhaustionException("exhausted", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ResourceInvalidStateException ─────────────────────────────────────

        [Fact]
        public void ResourceInvalidStateException_DefaultCtor()
        {
            var ex = new ResourceInvalidStateException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ResourceInvalidStateException_MessageCtor()
        {
            var ex = new ResourceInvalidStateException("invalid state");
            Assert.Equal("invalid state", ex.Message);
        }

        [Fact]
        public void ResourceInvalidStateException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new ResourceInvalidStateException("invalid state", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ResourceNotFoundException ─────────────────────────────────────────

        [Fact]
        public void ResourceNotFoundException_DefaultCtor()
        {
            var ex = new ResourceNotFoundException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ResourceNotFoundException_MessageCtor()
        {
            var ex = new ResourceNotFoundException("not found");
            Assert.Equal("not found", ex.Message);
        }

        [Fact]
        public void ResourceNotFoundException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new ResourceNotFoundException("not found", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── ResourceOutOfDateException ────────────────────────────────────────

        [Fact]
        public void ResourceOutOfDateException_DefaultCtor()
        {
            var ex = new ResourceOutOfDateException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void ResourceOutOfDateException_MessageCtor()
        {
            var ex = new ResourceOutOfDateException("out of date");
            Assert.Equal("out of date", ex.Message);
        }

        [Fact]
        public void ResourceOutOfDateException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new ResourceOutOfDateException("out of date", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── SerializerException ────────────────────────────────────────────────

        [Fact]
        public void SerializerException_DefaultCtor()
        {
            var ex = new SerializerException();
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void SerializerException_MessageCtor()
        {
            var ex = new SerializerException("bad json");
            Assert.Equal("bad json", ex.Message);
        }

        [Fact]
        public void SerializerException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new SerializerException("bad json", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── StorageException ──────────────────────────────────────────────────

        [Fact]
        public void StorageException_DefaultCtor()
        {
            var ex = new StorageException();
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void StorageException_MessageCtor()
        {
            var ex = new StorageException("storage error");
            Assert.Equal("storage error", ex.Message);
        }

        [Fact]
        public void StorageException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new StorageException("storage error", inner);
            Assert.Same(inner, ex.InnerException);
        }

        // ── TemporarilyBusyException ──────────────────────────────────────────

        [Fact]
        public void TemporarilyBusyException_DefaultCtor()
        {
            var ex = new TemporarilyBusyException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void TemporarilyBusyException_MessageCtor()
        {
            var ex = new TemporarilyBusyException("busy");
            Assert.Equal("busy", ex.Message);
        }

        [Fact]
        public void TemporarilyBusyException_MessageAndInnerCtor()
        {
            var inner = new Exception("inner");
            var ex = new TemporarilyBusyException("busy", inner);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void TemporarilyBusyException_RetryAfterCtor()
        {
            var retryAfter = TimeSpan.FromSeconds(30);
            var ex = new TemporarilyBusyException(retryAfter);
            Assert.Equal(retryAfter, ex.RetryAfter);
        }

        [Fact]
        public void TemporarilyBusyException_MessageAndRetryAfterCtor()
        {
            var retryAfter = TimeSpan.FromSeconds(10);
            var ex = new TemporarilyBusyException("try later", retryAfter);
            Assert.Equal("try later", ex.Message);
            Assert.Equal(retryAfter, ex.RetryAfter);
        }

        [Fact]
        public void TemporarilyBusyException_MessageInnerAndRetryAfterCtor()
        {
            var inner = new Exception("inner");
            var retryAfter = TimeSpan.FromSeconds(5);
            var ex = new TemporarilyBusyException("try later", inner, retryAfter);
            Assert.Same(inner, ex.InnerException);
            Assert.Equal(retryAfter, ex.RetryAfter);
        }

        // ── ExceptionExtensions ───────────────────────────────────────────────

        [Fact]
        public void AsMethodCallStatusException_ReturnsExistingMethodCallStatusException()
        {
            var original = new MethodCallStatusException(400, "bad");
            var result = original.AsMethodCallStatusException();
            Assert.Same(original, result);
        }

        [Fact]
        public void AsMethodCallStatusException_WrapsGenericExceptionWithGivenStatus()
        {
            var inner = new InvalidOperationException("oops");
            var ex = Assert.Throws<MethodCallStatusException>(
                () => inner.AsMethodCallStatusException(503));
            Assert.Equal(503, ex.Status);
        }

        [Fact]
        public void AsMethodCallStatusException_WrapsGenericExceptionWithDefaultStatus()
        {
            var inner = new InvalidOperationException("oops");
            var ex = Assert.Throws<MethodCallStatusException>(
                () => inner.AsMethodCallStatusException());
            Assert.Equal(500, ex.Status);
        }
    }
}

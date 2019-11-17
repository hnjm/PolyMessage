﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using PolyMessage.CodeGeneration;
using PolyMessage.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace PolyMessage.Tests.Micro
{
    public class ILEmitterTests : BaseFixture
    {
        private readonly CastTaskOfObjectToTaskOfResponse _targetMethod;

        public ILEmitterTests(ITestOutputHelper output) : base(output)
        {
            ICodeGenerator target = new ILEmitter();
            target.GenerateCode(new List<Operation>
            {
                new Operation {ResponseID = 456, ResponseType = typeof(Response1)},
                new Operation {ResponseID = 123, ResponseType = typeof(Response2)}
            });
            _targetMethod = target.GetCastTaskOfObjectToTaskOfResponseDelegate();
        }

        [Fact]
        public void CastingShouldSucceed()
        {
            // arrange
            Response1 response1 = new Response1();
            Response2 response2 = new Response2();

            // act
            Task<Response1> response1Task = (Task<Response1>) _targetMethod(456, Task.Run(() => (object) response1));
            Task<Response2> response2Task = (Task<Response2>) _targetMethod(123, Task.Run(() => (object) response2));

            // assert
            using (new AssertionScope())
            {
                response1Task.IsCompletedSuccessfully.Should().BeTrue();
                response1Task.Exception.Should().BeNull();
                response1Task.Result.Should().BeSameAs(response1);

                response2Task.IsCompletedSuccessfully.Should().BeTrue();
                response2Task.Exception.Should().BeNull();
                response2Task.Result.Should().BeSameAs(response2);
            }
        }

        [Fact]
        public void CastShouldThrowWhenMessageIDIsUnknown()
        {
            // arrange
            int unknownMessageID = -1;

            // act
            Action act = () => _targetMethod(unknownMessageID, Task.FromResult((object) new Response1()));

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage($"*{unknownMessageID}*");
        }
    }

    public static class UsedJustToSeeGeneratedIL
    {
        public static Task Cast(int messageID, Task<object> taskObject)
        {
            switch (messageID)
            {
                case 123: return CastPlaceHolder.GenericCast<Response1>(taskObject);
                case 234: return CastPlaceHolder.GenericCast<Response2>(taskObject);
                case 345: return CastPlaceHolder.GenericCast<Response3>(taskObject);
                case 456: return CastPlaceHolder.GenericCast<Response4>(taskObject);
                case 567: return CastPlaceHolder.GenericCast<Response5>(taskObject);
                case 678: return CastPlaceHolder.GenericCast<Response6>(taskObject);
                case 789: return CastPlaceHolder.GenericCast<Response7>(taskObject);
                default: throw new InvalidOperationException($"Unknown message ID {messageID}.");
            }
        }
    }

    public static class CastPlaceHolder
    {
        public static async Task<TResponse> GenericCast<TResponse>(Task<object> taskObject)
        {
            return (TResponse) await taskObject.ConfigureAwait(false);
        }
    }

    public sealed class Request1 {}
    public sealed class Response1 {}
    public sealed class Request2 {}
    public sealed class Response2 {}
    public sealed class Request3 {}
    public sealed class Response3 {}
    public sealed class Request4 {}
    public sealed class Response4 {}
    public sealed class Request5 {}
    public sealed class Response5 {}
    public sealed class Request6 {}
    public sealed class Response6 {}
    public sealed class Request7 {}
    public sealed class Response7 {}
}
using FluentAssertions;
using Microsoft.Playwright;
using Scriptube.Automation.Core.Configuration;
using Scriptube.Automation.Ui.HttpRetry;

namespace Scriptube.Automation.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="UiRetryPolicyFactory"/> verifying that the built pipeline
/// retries on <see cref="PlaywrightException"/> and <see cref="TimeoutException"/> the
/// configured number of times, and does not retry on unhandled exceptions.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class UiRetryPolicyFactoryTests
{
    private static RetrySettings FastRetry(int count) =>
        new() { Count = count, DelaySeconds = 0 };

    [Test]
    public async Task PlaywrightException_IsRetried_ConfiguredNumberOfTimes()
    {
        // Arrange
        var callCount = 0;
        var settings = FastRetry(count: 2);
        var policy = UiRetryPolicyFactory.BuildPolicy(settings);

        // Act — always throws, so all retries are exhausted
        var act = async () => await policy.ExecuteAsync(_ =>
        {
            callCount++;
            throw new PlaywrightException("element not visible");
#pragma warning disable CS0162 // Unreachable code: required for the ValueTask return type
            return ValueTask.CompletedTask;
#pragma warning restore CS0162
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<PlaywrightException>();

        // Assert — 1 initial attempt + 2 retries
        callCount.Should().Be(3,
            because: "MaxRetryAttempts=2 → 1 initial + 2 retries = 3 total invocations");
    }

    [Test]
    public async Task TimeoutException_IsRetried_ConfiguredNumberOfTimes()
    {
        // Arrange
        var callCount = 0;
        var settings = FastRetry(count: 2);
        var policy = UiRetryPolicyFactory.BuildPolicy(settings);

        // Act
        var act = async () => await policy.ExecuteAsync(_ =>
        {
            callCount++;
            throw new TimeoutException("navigation timed out");
#pragma warning disable CS0162
            return ValueTask.CompletedTask;
#pragma warning restore CS0162
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<TimeoutException>();
        callCount.Should().Be(3);
    }

    [Test]
    public async Task UnhandledException_IsNotRetried()
    {
        // Arrange
        var callCount = 0;
        var settings = FastRetry(count: 3);
        var policy = UiRetryPolicyFactory.BuildPolicy(settings);

        // Act — InvalidOperationException is not in the ShouldHandle predicate
        var act = async () => await policy.ExecuteAsync(_ =>
        {
            callCount++;
            throw new InvalidOperationException("not a retriable UI error");
#pragma warning disable CS0162
            return ValueTask.CompletedTask;
#pragma warning restore CS0162
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        callCount.Should().Be(1, because: "unhandled exceptions must not be retried");
    }

    [Test]
    public async Task PolicySucceeds_WhenActionEventuallySucceeds()
    {
        // Arrange — fails twice then succeeds
        var callCount = 0;
        var settings = FastRetry(count: 3);
        var policy = UiRetryPolicyFactory.BuildPolicy(settings);

        // Act
        await policy.ExecuteAsync(_ =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new PlaywrightException("not ready yet");
            }

            return ValueTask.CompletedTask;
        }, CancellationToken.None).AsTask();

        // Assert
        callCount.Should().Be(3, because: "two failures then one success = 3 calls total");
    }
}

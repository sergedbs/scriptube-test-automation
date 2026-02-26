using NUnit.Framework;

// Run test fixtures in parallel across all areas (API, UI, Webhook).
// Each fixture gets its own browser context (UI) or RestSharp client (API),
// so there is no shared mutable state between fixture classes.
//
// WebhookSmokeTests and WebhookLifecycleTests are marked [NonParallelizable]
// because they share a single WebhookReceiverManager / ReceivedRequestStore
// static singleton — concurrent fixture execution would cause test-isolation
// failures when one fixture's [SetUp] clears the other's pending requests.
[assembly: Parallelizable(ParallelScope.Fixtures)]

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-02-24

### Added

#### Project Structure

- Solution with five projects: `Core`, `Api`, `Ui`, `Webhooks` (under `src/`) and `Tests` (under `tests/`)
- Project references wired: `Tests → Api → Core`, `Tests → Ui → Core`, `Tests → Webhooks → Core`
- `TreatWarningsAsErrors` enabled on all `src/` projects to enforce a clean build

#### Configuration System (`Scriptube.Automation.Core`)

- `TestSettings` — strongly-typed POCO covering `BaseUrl`, `ApiKey`, `Credentials`, `Timeouts`, `Retry`, `Browser`, and `WebhookReceiverUrl`
- `ConfigurationProvider` — layered config loader: `appsettings.json` → `appsettings.{env}.json` → `.env` file → environment variables (highest priority wins)
- `appsettings.json` — committed default schema with safe placeholder values; copied to output directory on build
- `.env.example` — template for local developer secrets (never committed)
- `dotenv.net` integration for automatic `.env` file loading in local runs

#### HTTP Infrastructure (`Scriptube.Automation.Core`)

- `LoggingHttpHandler` — `DelegatingHandler` that captures every request/response exchange; masks `X-API-Key` header value in all log output; fires `OnExchange` event for downstream consumers
- `HttpExchange` record — immutable snapshot of a single HTTP round-trip (method, URL, headers, body, status code, elapsed ms)
- `ApiClientBase` — `RestClient` wrapper that injects `X-API-Key` on every request, pipes traffic through `LoggingHttpHandler`, and exposes `ExecuteAsync<T>` / `ExecuteAsync`

#### Allure Reporting (`Scriptube.Automation.Core`)

- `AllureRestLogger` — subscribes to `ApiClientBase.OnExchange` and attaches each HTTP exchange as an Allure step with a formatted `text/plain` attachment (request headers + body, response status + body, elapsed time)

#### Base Test Fixtures (`Scriptube.Automation.Core`)

- `GlobalSetupFixture` — NUnit `[SetUpFixture]` that initialises Serilog structured console logging once per assembly run
- `BaseTest` — abstract base class that loads `TestSettings` in `[SetUp]` and exposes it to all derived test classes

#### API Test Base (`Scriptube.Automation.Api`)

- `BaseApiTest : BaseTest` — creates an authenticated `ApiClientBase`, attaches `AllureRestLogger`, and disposes the client in `[TearDown]`

#### UI Test Base (`Scriptube.Automation.Ui`)

- `BaseUiTest : BaseTest` — launches a headless Chromium browser via Playwright per test; each test receives an isolated `IBrowserContext` and `IPage`; takes a full-page screenshot on failure and attaches it to the Allure report; cleans up browser resources in teardown
- `Allure.Net.Commons` added to the `Ui` project for screenshot attachment support

#### Webhook Test Base (`Scriptube.Automation.Webhooks`)

- `BaseWebhookTest : BaseTest` — provides an authenticated `ApiClientBase` with Allure logging; placeholder for HMAC verifier and receiver helpers (Iteration 6)

#### Repository Hygiene

- `.gitignore` — extended with automation-specific rules: `allure-results/`, `allure-report/`, `playwright-screenshots/`, `playwright/.cache/`, `appsettings.local.json`, `appsettings.*.local.json`
- `allureConfig.json` — Allure output directory and report title configured for the test project
- `README.md` — project overview, tech stack table, prerequisites, local setup instructions, environment variable reference, CI usage examples, and project structure map

### Changed

- Removed placeholder `Class1.cs` from all `src/` projects and `UnitTest1.cs` from the test project

[Unreleased]: https://github.com/sergedbs/scriptube-test-automation/compare/initial...HEAD
[0.1.0]: https://github.com/sergedbs/scriptube-test-automation/compare/initial...HEAD

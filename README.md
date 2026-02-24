# Scriptube Test Automation

End-to-end test automation framework for [scriptube.me](https://scriptube.me) — covering API, UI, and Webhook testing.

## Tech Stack

| Layer | Library |
| ----- | ------- |
| Language / Runtime | C# / .NET 10 |
| Test runner | NUnit 4 |
| HTTP client | RestSharp |
| UI automation | Playwright for .NET (Chromium) |
| Assertions | FluentAssertions |
| Reporting | Allure → GitHub Pages |
| CI | GitHub Actions |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Allure CLI](https://docs.qameta.io/allure/#_installing_a_commandline) (for local report generation)
- Playwright browsers: installed automatically on first `dotnet build` or via `pwsh playwright.ps1 install chromium`

## Local Setup

```bash
# 1. Clone
git clone https://github.com/sergedbs/scriptube-test-automation.git
cd scriptube-test-automation

# 2. Copy and fill secrets
cp .env.example .env
# Edit .env — set SCRIPTUBE_API_KEY, SCRIPTUBE_EMAIL, SCRIPTUBE_PASSWORD

# 3. Restore & build
dotnet restore
dotnet build

# 4. Run smoke tests
dotnet test --filter "Category=Smoke"

# 5. Run all tests
dotnet test
```

## Environment Variables

| Variable | Description | Required |
| -------- | ----------- | -------- |
| `SCRIPTUBE_API_KEY` | API key from the dashboard | ✅ |
| `SCRIPTUBE_EMAIL` | Login email | ✅ for UI tests |
| `SCRIPTUBE_PASSWORD` | Login password | ✅ for UI tests |
| `WEBHOOK_RECEIVER_URL` | HTTPS URL to receive webhook deliveries | ✅ for webhook tests |
| `SCRIPTUBE_BASE_URL` | Override base URL (default: `https://scriptube.me`) | ⬜ |
| `TEST_ENV` | Config environment (`Development` / `Production`) | ⬜ |

## Running via CI (GitHub Actions)

Trigger manually with parameters:

```txt
area:    api | ui | webhook | all
suite:   smoke | regression
threads: 1 | 2 | 4
```

Example `gh` CLI call:

```bash
gh workflow run test.yml \
  -f area=api \
  -f suite=smoke \
  -f threads=2
```

## Test Report

Latest Allure report: _link_

## Project Structure

```txt
src/
  Scriptube.Automation.Core/      # Config, HTTP client, base fixtures, logging
  Scriptube.Automation.Api/       # API service clients + base API test class
  Scriptube.Automation.Ui/        # Playwright page objects + base UI test class
  Scriptube.Automation.Webhooks/  # Webhook clients + base webhook test class
tests/
  Scriptube.Automation.Tests/     # All test classes (API, UI, Webhook)
```

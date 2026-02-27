# Scriptube Test Automation

End-to-end test automation framework for [scriptube.me](https://scriptube.me) — a YouTube transcript extraction SaaS. Covers API, UI, and Webhook testing with full CI/CD integration and Allure reporting.

## Tech Stack

| Layer | Technology |
| ----- | ---------- |
| Language / Runtime | C# / .NET 10 |
| Test runner | NUnit 4 |
| HTTP client | RestSharp |
| UI automation | Playwright for .NET (Chromium) |
| Assertions | FluentAssertions |
| Reporting | Allure → GitHub Pages |
| CI/CD | GitHub Actions |

## What It Tests

| Area | Smoke | Regression |
| ---- | ----- | ---------- |
| **API** | Auth, credits balance, user info, plans, validation errors | Full transcript lifecycle (submit → poll → export), credit deduction, all 11 success video paths, all 7 error video paths, playlists, usage endpoints |
| **UI** | Credits display, pricing page | Login flows, batch submission, transcript preview, JSON/TXT export, navigation |
| **Webhooks** | Registration, SSRF rejection, available events | Full lifecycle, HMAC-SHA256 verification, delivery logs, retry, batch completion events |

## Project Structure

```txt
src/
  Scriptube.Automation.Core/      # Config, HTTP client, base test fixtures, Allure logging
  Scriptube.Automation.Api/       # API service clients, DTOs, request builders
  Scriptube.Automation.Ui/        # Playwright page objects, browser factory, auth state
  Scriptube.Automation.Webhooks/  # Webhook client, HMAC verifier, local HTTP receiver
tests/
  Scriptube.Automation.Tests/     # All test classes (API, UI, Webhook) — Smoke & Regression
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Allure CLI](https://docs.qameta.io/allure/#_installing_a_commandline) — for local report generation
- [ngrok](https://ngrok.com/) — optional, only needed for local webhook end-to-end tests with HMAC verification

Playwright browsers are installed automatically on the first build.

## Local Setup

```bash
# 1. Clone
git clone https://github.com/sergedbs/scriptube-test-automation.git
cd scriptube-test-automation

# 2. Copy and fill in secrets
cp .env.example .env
# Edit .env — at minimum set SCRIPTUBE_API_KEY, SCRIPTUBE_EMAIL, SCRIPTUBE_PASSWORD

# 3. Restore & build
dotnet restore
dotnet build
```

## Running Tests

Use the included `run-tests.sh` script, which mirrors the CI pipeline locally (quality gate → build → test → Allure report):

```bash
# Smoke tests across all areas (default)
./run-tests.sh

# API smoke only
./run-tests.sh --area api --suite smoke

# Full API regression, 4 parallel workers
./run-tests.sh --area api --suite regression --threads 4

# All areas, all suites, open report when done
./run-tests.sh --area all --suite all --threads 2 --open-report

# First-time setup — install Playwright browsers
./run-tests.sh --install-playwright --suite smoke
```

Available options:

| Option | Values | Default |
| ------ | ------ | ------- |
| `--area` | `api` \| `ui` \| `webhook` \| `all` | `all` |
| `--suite` | `smoke` \| `regression` \| `all` | `smoke` |
| `--threads` | any integer | `1` |
| `--skip-quality` | — | off |
| `--no-report` | — | off |
| `--open-report` | — | off |
| `--install-playwright` | — | off |

## Environment Variables

| Variable | Description | Required |
| -------- | ----------- | -------- |
| `SCRIPTUBE_API_KEY` | API key from the dashboard | ✅ |
| `SCRIPTUBE_EMAIL` | Login email | ✅ for UI tests |
| `SCRIPTUBE_PASSWORD` | Login password | ✅ for UI tests |
| `WEBHOOK_RECEIVER_URL` | External HTTPS URL for webhook delivery (skips ngrok) | ✅ for webhook E2E |
| `SCRIPTUBE_BASE_URL` | Override base URL | default: `https://scriptube.me` |
| `NGROK_AUTHTOKEN` | ngrok auth token for local webhook tunnelling | optional |
| `BROWSER_HEADLESS` | Run browser headless | default: `true` |
| `BROWSER_SLOW_MO` | Playwright slow-motion delay in ms | default: `0` |

All timeouts and retry counts are also configurable — see `.env.example` for the full list.

## CI/CD — GitHub Actions

The pipeline runs four jobs in sequence: **Quality Gate → Smoke → Regression → Publish Report**.

| Trigger | Behaviour |
| ------- | --------- |
| Push to `main` | Quality gate + Smoke suite |
| Pull request | Quality gate + Smoke suite |
| Manual dispatch | Configurable area, suite, and thread count |

Trigger a specific run via the GitHub UI or `gh` CLI:

```bash
gh workflow run test.yml \
  -f area=api \
  -f suite=regression \
  -f threads=2
```

Required GitHub Secrets: `SCRIPTUBE_API_KEY`, `SCRIPTUBE_EMAIL`, `SCRIPTUBE_PASSWORD`, `NGROK_AUTHTOKEN`.

## Test Report

The Allure report is published to GitHub Pages after every CI run and includes full HTTP request/response logs, per-step traces, and screenshots on UI test failure.

**Latest report:** [https://sergedbs.github.io/scriptube-test-automation/](https://sergedbs.github.io/scriptube-test-automation/)

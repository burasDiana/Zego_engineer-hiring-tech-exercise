# Web Crawler (.NET 10)

A concurrent, domain-restricted web crawler implemented in .NET 10.

The solution is structured as a multi-project .NET repository separating core logic, CLI interface, and tests.

## Project Structure

```text
/Crawler.sln
/src
  /Crawler.Core
  /Crawler.Cli
/tests
  /Crawler.Tests
Dockerfile
.editorconfig
.gitignore
README.md
```
## Components
- Crawler.Core: Crawling engine, concurrency control, URL normalization, `robots.txt` handling.

- Crawler: Command-line interface, argument parsing, dependency injection, logging.

- Crawler.Tests: Unit tests for normalization, deduplication, concurrency behavior, and parsing.

## Usage

### build the docker image

    docker build -t crawler .
### run the crawler with a starting URL

    docker run crawler "https://test.com"

## Arguments
| Flag                  | Description                                    |
|-----------------------|------------------------------------------------|
 no flag needed        | Starting URL (required)                        |
| `--concurrency`, `-c` | Maximum number of concurrent tasks; default:8  |
| `--no-robots`, `-nr`  | Enables `robots.txt` compliance ; default:true |
| `--debug`, `-d`       | Enables verbose logging ; default:false        |

### example run with custom concurrency, no robots adherence and debug logging enabled:

    docker run crawler "https://test.com" -nr -d -c 10

## Tools and NuGet Packages

- AngleSharp - HTML parsing and link extraction
- System.Net.Http.HttpClient — asynchronous HTTP requests
- System.Collections.Concurrent — thread-safety
- Microsoft.Extensions.Logging
- Microsoft.Extensions.DependencyInjection
- xUnit - testing framework
- FluentAssertions - assertion library for tests

### Use of AI tools
Claude Opus 4.5 was used to assist with:
- ReadMe formatting and structuring
- Code review and refactoring suggestions
- Test case generation ideas

## Architecture and Design

### Console APP vs Service

The crawler is implemented as a console application for simplicity and ease of use. This allows users to run it directly from the command line or within a Docker container without needing to set up additional infrastructure. A production grade crawler would likely expose an API or run as a distributed service.
### Regex vs HTML Parser
The crawler uses the AngleSharp library for HTML parsing and link extraction instead of regular expressions.
This choice is made because HTML is a complex and often malformed language that can easily break regex-based parsers. AngleSharp provides a robust and standards-compliant way to parse HTML, handle edge cases, and extract links accurately.

### Satisfy requirements
The crawler is designed to meet the following requirements:
- **Domain Restriction**: Only processes URLs within the same domain as the starting URL (using exact host matching so subdomains are not included).
- **Robots.txt Compliance**: Respects `robots.txt` rules by default, with an option to disable this behavior.
- **Concurrency**: Uses a worker pool to process multiple URLs concurrently.
- **URL Normalization and Deduplication**: Normalizes URLs to a canonical form and uses a thread-safe set to avoid processing duplicates.
- **Fault Tolerance**: Implements retry logic with backoff for transient network errors and server erorrs.
- **Real‑Time Output**: Streams discovered URLs immediately through the `onResult` callback to provide incremental, low‑latency visibility into crawl progress.
- **Protocol Restriction**: Only processes `http` and `https` URLs, ignoring all other schemes to ensure safe, valid web crawling.

### Concurrency Control

The crawler uses a bounded‑concurrency worker model implemented entirely with .NET’s built‑in concurrency primitives (ConcurrentQueue<T>, Interlocked, and Task.Run).
The goal is to maximize throughput while ensuring deterministic shutdown, thread‑safe state management, and predictable resource usage

The crawler uses a fixed‑size worker pool.
`maxConcurrency` determines how many workers are created, and each worker runs independently until the crawl completes.

### Worker Lifecycle

Each worker runs the same loop:

1. Try to dequeue a URL
    - If successful → process it.
    - If not → check whether the crawl is finished.

2. Increment `activeWorkers`
    - Signals that this worker is now busy.

3. Process the URL
    - Fetch the page content
    - Extract links
    - Normalize and dedupe
    - Enqueue new URLs

4. Decrement `activeWorkers`
    - Signals that this worker has completed its unit of work.

5. Termination condition
    - If the queue is empty **and** `activeWorkers == 0`, the crawl is complete.
    - Worker exits its loop.

&rarr; scales linearly

Increasing maxConcurrency increases throughput until external bottlenecks (network, target server) dominate.

&rarr; deterministic shutdown

Workers only exit when either:

- the queue is empty

- no workers are active

## Fault-Tolerance and Robustness

The crawler includes explicit retry and backoff logic to handle transient network failures and server‑side instability. This logic is implemented in the `HttpClientWrapper` class, inside the `GetWithRetryAsync` method, which is responsible for performing HTTP requests in a resilient manner.

### Retry Logic

`GetWithRetryAsync` retries requests that fail due to transient conditions, including:

- HTTP 5xx responses (e.g., 500, 502, 503, 504)
- Timeouts
- Connection resets
- DNS resolution failures
- Other intermittent transport‑level exceptions

### Backoff Strategy

Retries are performed with a backoff delay to avoid overwhelming the target server and to give it time to recover. The backoff achieves the following:

- Increases the delay between attempts
- Reduces retry swarming
- Improves politeness toward the target domain
- Helps stabilize crawling under fluctuating network conditions

### Bounded Retries

To prevent infinite retry loops, `GetWithRetryAsync` enforces:

- A maximum retry count
- A final failure path if all attempts are exhausted

This prevents workers from stalling indefinitely.

### Testing Strategy
Tests cover:
- core functionality of the crawler

- URL normalization

- Deduplication logic

- Concurrency behavior

- Robots.txt handling

- parser url extraction

- Worker shutdown logic

Tests run during Docker build and are a prerequisite for a successful build, ensuring code quality and reliability.

## Future Extensions
### Add depth limits, max pages, or domain limits
- Implement `maxDepth` to restrict how deep the crawler goes into a website's link structure to prevent the crawler expaning exponentially.
### Add metrics / instrumentation
- log crawl progress and errors to a monitoring system (Azure Application Insights, Prometheus, etc.)
- average run time, number of pages crawled, etc. (useful for performance tuning and debugging)
### Add results persistence

- store visited URLs in a database
- store crawl results and metadata about a crawl instance

### Add integration tests
Mock an HTTP server to simulate various crawling scenarios, including different HTML structures, `robots.txt` configurations, and error conditions.

## Discussion of Production-Grade Crawler Design
### Multi-Domain Crawling
Supporting multi‑domain crawling requires externalizing state and introducing per‑domain isolation so that concurrency, politeness, and deduplication are no longer global
but domain‑scoped. Instead of a single queue and a single visited set, each domain would maintain its own work queue, visited cache, and rate‑limit,
typically stored in a distributed system such as Redis, DynamoDB, or a sharded keyspace. Workers would pull tasks tagged with a domain key,
enforce per‑domain concurrency, and apply domain‑specific robots.txt rules. This prevents one domain from starving others and ensures fair scheduling across many hosts.


"""Operational API contracts ported from ``src/UI/Api``.

The Python host reports equivalent process/runtime information where Python has a
direct analogue. It deliberately returns environment-variable names only and
redacts values, matching the source endpoint's safety contract.
"""
import hashlib
import json
import os
import platform
import sys
import threading
import time
from datetime import datetime, timezone
from pathlib import Path

from django.conf import settings
from django.db import connection
from django.http import HttpResponseNotAllowed, JsonResponse

PROCESS_STARTED = time.monotonic()
REQUESTS_SERVED = 0
REQUESTS_LOCK = threading.Lock()
REDACTED = "[REDACTED]"
REPORTED_ENVIRONMENT_VARIABLES = (
    "ASPNETCORE_ENVIRONMENT",
    "APPLICATIONINSIGHTS_CONNECTION_STRING",
    "DOTNET_ENVIRONMENT",
    "DOTNET_ROOT",
    "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",
    "TEST_ENV_STATUS_SECRET",
)
FEATURE_FLAGS = {"SampleFeatureA": True, "SampleFeatureB": False}


class RequestMetricsMiddleware:
    """Count each HTTP request from process start, like the source middleware."""

    def __init__(self, get_response):
        self.get_response = get_response

    def __call__(self, request):
        global REQUESTS_SERVED
        with REQUESTS_LOCK:
            REQUESTS_SERVED += 1
        return self.get_response(request)


def _duration():
    seconds = max(0, time.monotonic() - PROCESS_STARTED)
    hours, remainder = divmod(seconds, 3600)
    minutes, remainder = divmod(remainder, 60)
    return f"{int(hours):02}:{int(minutes):02}:{remainder:06.3f}"


def _json(request, payload, *, conditional=True, etag_payload=None):
    body = json.dumps(payload, sort_keys=True, separators=(",", ":"), default=str).encode()
    fingerprint = body if etag_payload is None else json.dumps(
        etag_payload, sort_keys=True, separators=(",", ":"), default=str
    ).encode()
    etag = 'W/"' + hashlib.sha256(fingerprint).hexdigest() + '"'
    if conditional:
        supplied = request.headers.get("If-None-Match", "")
        candidates = (part.strip() for part in supplied.split(","))
        if any(candidate == "*" or candidate.removeprefix("W/") == etag.removeprefix("W/") for candidate in candidates):
            response = JsonResponse({}, status=304)
            response.content = b""
            response["ETag"] = etag
            return response
    response = JsonResponse(payload, json_dumps_params={"sort_keys": True})
    if conditional:
        response["ETag"] = etag
    return response


def _environment_payload():
    names = [name for name in REPORTED_ENVIRONMENT_VARIABLES if name in os.environ]
    values = {name: REDACTED for name in names}
    return {
        "osDescription": platform.platform(),
        "processorCount": os.cpu_count() or 1,
        "clrVersion": platform.python_version(),
        "environmentVariableNames": names,
        "environmentVariables": values,
        "version": os.getenv("APP_VERSION", "unknown"),
        "gitSha": os.getenv("GIT_SHA", "unknown"),
        "environmentName": os.getenv("ASPNETCORE_ENVIRONMENT", os.getenv("DOTNET_ENVIRONMENT", "unknown")),
    }


def _echo_payload(request):
    headers = {}
    for name, value in request.headers.items():
        headers[name] = REDACTED if name.lower() in {"authorization", "x-api-key", "cookie"} else value
    return {
        "method": request.method,
        "path": request.path,
        "queryString": f"?{request.META['QUERY_STRING']}" if request.META.get("QUERY_STRING") else "",
        "scheme": request.scheme,
        "host": request.get_host(),
        "protocol": request.META.get("SERVER_PROTOCOL", ""),
        "remoteIpAddress": request.META.get("REMOTE_ADDR"),
        "headers": headers,
    }


def _memory_bytes():
    try:
        pages = int(Path("/proc/self/statm").read_text().split()[1])
        return pages * os.sysconf("SC_PAGE_SIZE")
    except (OSError, ValueError, IndexError):
        return 0


def _payload(request, endpoint):
    if endpoint == "health":
        return {"status": "Healthy", "currentTimeUtc": datetime.now(timezone.utc), "uptime": _duration()}
    if endpoint == "health/detailed":
        try:
            connection.ensure_connection()
            component = {"name": "database", "status": "Healthy"}
            overall = "Healthy"
        except Exception as error:
            component = {"name": "database", "status": "Unhealthy", "exceptionMessage": str(error)}
            overall = "Unhealthy"
        memory = _memory_bytes()
        return {
            "overallStatus": overall,
            "checkedAtUtc": datetime.now(timezone.utc),
            "processId": os.getpid(),
            "osDescription": platform.platform(),
            "frameworkDescription": f"Python {platform.python_version()}",
            "gcMemoryMb": 0,
            "workingSetMb": round(memory / 1_048_576),
            "processorCount": os.cpu_count() or 1,
            "is64BitProcess": sys.maxsize > 2**32,
            "timeZoneId": str(datetime.now().astimezone().tzinfo),
            "processPriority": "Normal",
            "components": [component],
        }
    if endpoint == "diagnostics":
        development = os.getenv("DJANGO_ENVIRONMENT", "Production").lower() == "development"
        return {
            "environment": os.getenv("DJANGO_ENVIRONMENT", "Production"),
            "uptime": _duration(),
            "featureFlags": {"sampleFeatureA": development, "sampleFeatureB": False},
        }
    if endpoint == "status/environment":
        return _environment_payload()
    if endpoint == "features/flags":
        return FEATURE_FLAGS
    if endpoint == "echo":
        return _echo_payload(request)
    if endpoint == "metrics/summary":
        memory = _memory_bytes()
        with REQUESTS_LOCK:
            requests_served = REQUESTS_SERVED
        return {
            "uptime": _duration(),
            "totalRequestsServed": requests_served,
            "workingSetBytes": memory,
            "managedMemoryBytes": memory,
            "gcGen0Collections": 0,
            "gcGen1Collections": 0,
            "gcGen2Collections": 0,
        }
    if endpoint == "version":
        return {
            "assemblyVersion": None,
            "informationalVersion": os.getenv("APP_VERSION", "unknown"),
            "buildConfiguration": "Debug" if settings.DEBUG else "Release",
            "environment": os.getenv("DJANGO_ENVIRONMENT", "Production"),
            "machineName": platform.node(),
            "frameworkDescription": f"Python {platform.python_version()}",
        }
    return None


def operational_api(request, path=""):
    if request.method != "GET":
        return HttpResponseNotAllowed(["GET"])
    endpoint = path.strip("/")
    if endpoint.startswith("v1.0/"):
        endpoint = endpoint[5:]
    payload = _payload(request, endpoint)
    if payload is None:
        return JsonResponse({"detail": "Not found."}, status=404)
    etag_payload = None
    if endpoint == "health/detailed":
        etag_payload = {
            "overallStatus": payload["overallStatus"],
            "components": payload["components"],
        }
    return _json(request, payload, conditional=endpoint != "echo", etag_payload=etag_payload)

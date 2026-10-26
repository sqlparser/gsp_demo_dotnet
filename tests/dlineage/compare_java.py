#!/usr/bin/env python3
"""Run the real Java demo and the unified C# CLI with identical SQL/options/metadata.

No IDs, coordinates, source order, schema names or diagnostics are erased. Raw output
and parsed XML/JSON equality are recorded separately. Differences always remain visible.
"""
import argparse
import concurrent.futures
import difflib
from decimal import Decimal
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
from types import SimpleNamespace
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SAMPLES = ROOT / "samples" / "dlineage"
MAIN = "gudusoft.gsqlparser.demos.dlineage.DataFlowAnalyzer"


def load_suite(path):
    """Load pinned original SQL/options without replacing the historical matrix."""
    suite = json.loads(path.read_text(encoding="utf-8"))
    if suite.get("schemaVersion") != 1 or not suite.get("cases"):
        raise ValueError("Expected a nonempty schemaVersion=1 case manifest")
    cases, names = [], set()
    for item in suite["cases"]:
        name = item["name"]
        if name in names or name.startswith("/") or "\\" in name or any(p in ("", ".", "..") for p in name.split("/")):
            raise ValueError("Invalid or duplicate case name: " + name)
        names.add(name)
        sql = (ROOT / item["sql"]).resolve()
        if hashlib.sha256(sql.read_bytes()).hexdigest() != item["sqlSha256"]:
            raise ValueError("Original SQL bytes changed: " + name)
        flags = item.get("flags", [])
        if not all(isinstance(flag, str) for flag in flags) or any(flag.lower() in ("/f", "/d", "/t", "/env", "/o") for flag in flags):
            raise ValueError("Input/output selectors must use manifest fields: " + name)
        options = ["/f", str(sql), "/t", item.get("vendor", "mssql"), *flags]
        if item.get("metadata"):
            metadata = (ROOT / item["metadata"]).resolve()
            if hashlib.sha256(metadata.read_bytes()).hexdigest() != item["metadataSha256"]:
                raise ValueError("Original metadata bytes changed: " + name)
            options += ["/env", str(metadata)]
        cases.append((name, options))
    return cases


def matrix():
    cases = []
    def add(name, flags=(), sample="advanced-oracle.sql", vendor="oracle"):
        cases.append((name, ["/f", str(SAMPLES / sample), "/t", vendor, *flags]))
    for dialect in ("oracle", "mssql", "mysql", "postgresql"):
        add("basic-" + dialect, sample="basic.sql", vendor=dialect)
    variants = {
        "full": [], "simple": ["/s"], "ignore-recordsets": ["/i"],
        "topselect": ["/s", "/topselectlist"], "without-functions": ["/if"],
        "selected-resultsets": ["/s", "/showResultSetTypes", "cte,function"],
        "simple-relations": ["/s", "/simpleShowRelationTypes", "fdd,fdr"],
        "retain-objects": ["/s", "/showVariable", "/showCursor", "/showSynonym"],
        "remove-objects": ["/removeResultSetTypes", "cte,function", "/removeVariable", "/removeCursor", "/removeUnusedSynonym"],
        "normalized": ["/n", "/ic", "/lof", "/j"],
        "transform-code": ["/transform", "/showConstant", "/traceTablePosition"],
        "transform-coordinates": ["/transform", "/coor", "/showConstant", "/traceTablePosition"],
        "case-count": ["/showCaseWhenAsIndirect", "/treatArgumentsInCountFunctionAsDirectDataflow"],
        "default-names": ["/defaultDatabase", "demo", "/defaultSchema", "sales", "/showImplicitSchema"],
        "filter-relations": ["/filterRelationTypes", "fdd"],
        "csv": ["/csv"], "csv-simple": ["/csv-simple"], "csv-delimiter": ["/csv", "/delimiter", "|"],
        "json": ["/json"], "graph": ["/json", "/graph"], "er-graph": ["/json", "/graph", "/showER"],
        "visualize": ["/lv"], "trace-view": ["/traceView"], "table-xml": ["/tableLineage"],
        "table-json": ["/tableLineage", "/json"], "table-csv": ["/tableLineage", "/csv"],
        "table-simple-csv": ["/tableLineage", "/csv-simple"],
        "table-graph": ["/tableLineage", "/json", "/graph"],
        "table-er-graph": ["/tableLineage", "/json", "/graph", "/showER"],
        "table-visualize": ["/tableLineage", "/lv"],
    }
    for name, flags in variants.items():
        add(name, flags)
    add("metadata", ["/env", str(SAMPLES / "metadata.json"), "/defaultDatabase", "demo_db", "/defaultSchema", "dbo", "/s"], "metadata-query.sql", "mssql")
    add("routine-full", [], "routine-oracle.sql")
    add("routine-retain", ["/s", "/showVariable", "/showCursor", "/showSynonym"], "routine-oracle.sql")
    add("routine-remove", ["/removeVariable", "/removeCursor", "/removeUnusedSynonym"], "routine-oracle.sql")
    add("temporary-retain", ["/s", "/withTemporaryTable"], "temporary-mssql.sql", "mssql")
    add("temporary-remove", ["/s"], "temporary-mssql.sql", "mssql")
    add("statistics", ["/stat"], "basic.sql")
    return cases


def shape(text):
    stripped = text.strip()
    if stripped.startswith("<"):
        def node(element):
            text = element.text or ""
            if len(element) and not text.strip():
                text = ""  # indentation between elements, not leaf/SQL text
            tail = element.tail or ""
            if not tail.strip():
                tail = ""
            return [element.tag, sorted(element.attrib.items()), text, [node(child) for child in element], tail]
        return node(ET.fromstring(stripped))
    if stripped.startswith("{") or stripped.startswith("["):
        def typed(value):
            if value is None: return ("null",)
            if isinstance(value, bool): return ("boolean", value)
            if isinstance(value, Decimal): return ("number", value)
            if isinstance(value, str): return ("string", value)
            if isinstance(value, list): return ("array", tuple(typed(item) for item in value))
            return ("object", tuple(sorted((key, typed(item)) for key, item in value.items())))
        return typed(json.loads(stripped, parse_int=Decimal, parse_float=Decimal))
    return stripped.replace("\r\n", "\n")


def statistics_counters(text):
    # These are the deterministic Java listener counters. Timing and status-line
    # durations are retained verbatim in stdout records but are not equality gates.
    keys = {"Total sql files", "Total sql statements", "Total sql lines", "Average statements per file",
            "Average lines per file", "Average lines per sql statement", "Statement passed", "Statement failed",
            "Files ignored in trial version"}
    result = {}
    for line in text.splitlines():
        key, separator, value = line.partition(":")
        if separator and key.strip() in keys:
            result[key.strip()] = value.strip()
    if set(result) != keys:
        raise ValueError("Missing statistics counters: " + str(keys - set(result)))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True, help="built dlineage.dll")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--java-classpath", help="real Java demo + parser + dependency classpath")
    group.add_argument("--java-jar", type=Path, help="standalone Java demo jar")
    group.add_argument("--java-records", type=Path, help="reuse saved Java outputs from an unchanged reference/input set")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--suite", type=Path, help="additional pinned SQL/options manifest; default historical matrix is unchanged")
    parser.add_argument("--startup-hook", type=Path, help="verification AssemblyLoad hook for the actual C# parser")
    parser.add_argument("--expected-parser-sha256")
    parser.add_argument("--expected-parser-tfm")
    parser.add_argument("--java-load-log", action="store_true", help="retain actual Java class-loading sources per fresh run")
    parser.add_argument("--case", action="append", help="restrict to named cases")
    args = parser.parse_args()
    if any((args.startup_hook, args.expected_parser_sha256, args.expected_parser_tfm)) and not all((args.startup_hook, args.expected_parser_sha256, args.expected_parser_tfm)):
        parser.error("--startup-hook, --expected-parser-sha256 and --expected-parser-tfm must be specified together")
    args.output.mkdir(parents=True, exist_ok=True)
    java = None if args.java_records else (["java", "-cp", args.java_classpath, MAIN] if args.java_classpath else ["java", "-jar", str(args.java_jar.resolve())])
    dotnet = ["dotnet", str(args.dotnet.resolve())]
    def run(case):
        name, options = case
        folder = args.output / name
        folder.mkdir(parents=True, exist_ok=True)
        record = {"name": name, "options": options}
        record["inputFiles"] = {options[index + 1]: hashlib.sha256(Path(options[index + 1]).read_bytes()).hexdigest()
                                for index, option in enumerate(options) if option in ("/f", "/env")}
        results = {}
        for runtime, prefix in (("java", java), ("dotnet", dotnet)):
            if runtime == "java" and args.java_records:
                source = args.java_records / name
                previous = json.loads((source / "result.json").read_text(encoding="utf-8"))
                if previous["options"] != options:
                    raise ValueError("Reference options/input paths changed: " + name)
                if previous.get("inputFiles") != record["inputFiles"]:
                    raise ValueError("Reference SQL/metadata bytes changed or reference lacks input fingerprints: " + name)
                result = SimpleNamespace(stdout=(source / "java.stdout").read_bytes(), stderr=(source / "java.stderr").read_bytes(), returncode=previous["java"]["exit"])
                if hashlib.sha256(result.stdout).hexdigest() != previous["java"]["stdoutSha256"] or hashlib.sha256(result.stderr).hexdigest() != previous["java"]["stderrSha256"]:
                    raise ValueError("Java reference record hash mismatch: " + name)
                record["javaReferenceReusedFrom"] = str(source.resolve())
            else:
                invocation = list(prefix)
                environment = {}
                if runtime == "java" and args.java_load_log:
                    invocation.insert(1, "-Xlog:class+load=info:file=" + str((folder / "java-loads.log").resolve()))
                if runtime == "dotnet" and args.startup_hook:
                    parser_path = args.dotnet.resolve().with_name("gudusoft.gsqlparser.dll")
                    environment = {
                        "DOTNET_STARTUP_HOOKS": str(args.startup_hook.resolve()),
                        "GSP_R244_ORIGINAL_LOAD_REPORT": str((folder / "dotnet-loaded.json").resolve()),
                        "GSP_DLINEAGE_EXPECTED_ASSEMBLY_SHA256": args.expected_parser_sha256,
                        "GSP_DLINEAGE_EXPECTED_TFM": args.expected_parser_tfm,
                        "GSP_DLINEAGE_EXPECTED_ASSEMBLY_PATH": str(parser_path),
                    }
                record[runtime + "Command"] = [*invocation, *options]
                result = subprocess.run([*invocation, *options], cwd=ROOT, env={**os.environ, **environment}, capture_output=True, timeout=90)
                if runtime == "dotnet" and args.startup_hook:
                    loaded = json.loads((folder / "dotnet-loaded.json").read_text())
                    if loaded["sha256"] != args.expected_parser_sha256 or loaded["framework"] != args.expected_parser_tfm or Path(loaded["location"]) != parser_path:
                        raise ValueError("Actual loaded parser differs from the requested artifact: " + name)
                    record["actualLoadedCore"] = loaded
            (folder / (runtime + ".stdout")).write_bytes(result.stdout)
            (folder / (runtime + ".stderr")).write_bytes(result.stderr)
            record[runtime] = {"exit": result.returncode, "stdoutSha256": hashlib.sha256(result.stdout).hexdigest(), "stderrSha256": hashlib.sha256(result.stderr).hexdigest()}
            results[runtime] = result
        left, right = results["java"], results["dotnet"]
        record["rawExact"] = left.stdout == right.stdout
        try:
            observer = statistics_counters if name == "statistics" else shape
            record["comparison"] = "statistics counters (timing recorded separately)" if name == "statistics" else "complete output model"
            record["structuredExact"] = observer(left.stdout.decode("utf-8")) == observer(right.stdout.decode("utf-8"))
        except Exception as error:
            record["structuredExact"] = False
            record["parseError"] = str(error)
        record["successful"] = left.returncode == right.returncode == 0
        if not record["rawExact"]:
            (folder / "output.diff").write_text("".join(difflib.unified_diff(left.stdout.decode("utf-8").splitlines(True), right.stdout.decode("utf-8").splitlines(True), fromfile="java", tofile="dotnet")), encoding="utf-8")
        (folder / "result.json").write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{name}: raw={record['rawExact']} structured={record['structuredExact']} exits={left.returncode}/{right.returncode}", flush=True)
        return record
    try:
        available = load_suite(args.suite) if args.suite else matrix()
    except (ValueError, KeyError, OSError) as error:
        parser.error(str(error))
    unknown = set(args.case or ()) - {name for name, _ in available}
    if unknown:
        parser.error("unknown cases: " + ", ".join(sorted(unknown)))
    cases = [case for case in available if not args.case or case[0] in args.case]
    if not cases: parser.error("no matching cases")
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        records = list(pool.map(run, cases))
    summary = {"cases": len(records), "rawExact": sum(r["rawExact"] for r in records), "structuredExact": sum(r["structuredExact"] for r in records), "successful": sum(r["successful"] for r in records), "records": records}
    if args.suite:
        summary["suite"] = {"path": str(args.suite.resolve()), "sha256": hashlib.sha256(args.suite.read_bytes()).hexdigest()}
    (args.output / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in summary.items() if k != "records"}))
    return 0 if all(r["successful"] and r["structuredExact"] for r in records) else 1


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env bash
#
# build.sh — Build and run General SQL Parser .NET demos
#
# Usage:
#   ./build.sh                   Build all demos
#   ./build.sh <demo>            Build a single demo
#   ./build.sh run <demo> [args] Build and run a single demo
#   ./build.sh clean             Remove all build output
#   ./build.sh list              List available demos
#
# Examples:
#   ./build.sh checksyntax
#   ./build.sh run formatsql /t oracle /f /path/to/query.sql
#   ./build.sh run checksyntax /t mssql

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SLNX="$SCRIPT_DIR/demos.slnx"

# All database dialect flags — enables every supported vendor in the parser.
INCLUDE_FLAGS=(
  /p:includeDB2=true
  /p:includeGreenplum=true
  /p:includeHive=true
  /p:includeImpala=true
  /p:includeInformix=true
  /p:includeMdx=true
  /p:includeMssql=true
  /p:includeMySQL=true
  /p:includeNetezza=true
  /p:includeOracle=true
  /p:includePostgreSQL=true
  /p:includeRedshift=true
  /p:includeSnowflake=true
  /p:includeSybase=true
  /p:includeTeradata=true
)

# Map of demo name -> csproj path (relative to SCRIPT_DIR)
declare -A DEMOS=(
  [analyzesp]="analyzesp/demos.analyzesp.csproj"
  [checksyntax]="checksyntax/demos.checksyntax.csproj"
  [columnImpact]="columnImpact/demos.columnImpact.csproj"
  [convertJoin]="convertJoin/demos.convertJoin.csproj"
  [dataFlowAnalyzer]="dataFlowAnalyzer/demos.dataFlowAnalyzer.csproj"
  [dlineage]="dlineage/demos.dlineage.csproj"
  [dlineageRelation]="dlineageRelation/demos.dlineageRelation.csproj"
  [expressionTraverser]="expressionTraverser/demos.expressionTraverser.csproj"
  [formatsql]="formatsql/demos.formatsql.csproj"
  [gettablecolumns]="gettablecolumns/demos.gettablecolumns.csproj"
  [joinRelationAnalyze]="joinRelationAnalyze/demos.joinRelationAnalyze.csproj"
  [listGSPInfo]="listGSPInfo/demos.listGSPInfo.csproj"
  [removeColumn]="removeColumn/demos.removeColumn.csproj"
  [removeCondition]="removeCondition/demos.removeCondition.csproj"
  [removevars]="removevars/demos.removevars.csproj"
  [tableColumnRename]="tableColumnRename/demos.tableColumnRename.csproj"
  [visitors]="visitors/demos.toXML.csproj"
)

usage() {
  cat <<'EOF'
Usage:
  ./build.sh                   Build all demos
  ./build.sh <demo>            Build a single demo
  ./build.sh run <demo> [args] Build and run a single demo
  ./build.sh clean             Remove all build output
  ./build.sh list              List available demos

Examples:
  ./build.sh checksyntax
  ./build.sh run formatsql /t oracle /f /path/to/query.sql
  ./build.sh run checksyntax /t mssql

Run ./build.sh list to see all available demo names.
EOF
}

list_demos() {
  echo "Available demos:"
  echo ""
  printf "  %-25s %s\n" "analyzesp"          "Analyze stored procedures"
  printf "  %-25s %s\n" "checksyntax"        "Check SQL syntax"
  printf "  %-25s %s\n" "columnImpact"       "Analyze column impact / lineage"
  printf "  %-25s %s\n" "convertJoin"        "Convert between join syntax styles"
  printf "  %-25s %s\n" "dataFlowAnalyzer"   "Analyze data flow in SQL"
  printf "  %-25s %s\n" "dlineage"           "Data lineage analysis"
  printf "  %-25s %s\n" "dlineageRelation"   "Data lineage relation analysis"
  printf "  %-25s %s\n" "expressionTraverser" "Traverse SQL expressions"
  printf "  %-25s %s\n" "formatsql"          "Format / pretty-print SQL"
  printf "  %-25s %s\n" "gettablecolumns"    "Extract table and column names"
  printf "  %-25s %s\n" "joinRelationAnalyze" "Analyze join relations"
  printf "  %-25s %s\n" "listGSPInfo"        "List parser version info"
  printf "  %-25s %s\n" "removeColumn"       "Remove columns from SQL statements"
  printf "  %-25s %s\n" "removeCondition"    "Remove conditions from WHERE clauses"
  printf "  %-25s %s\n" "removevars"         "Remove bind variables from SQL"
  printf "  %-25s %s\n" "tableColumnRename"  "Rename table/column references"
  printf "  %-25s %s\n" "visitors"           "AST visitor / XML export"
}

resolve_csproj() {
  local name="$1"
  local csproj="${DEMOS[$name]:-}"
  if [[ -z "$csproj" ]]; then
    echo "Error: unknown demo '$name'" >&2
    echo "" >&2
    list_demos >&2
    exit 1
  fi
  echo "$SCRIPT_DIR/$csproj"
}

build_all() {
  echo "Building all demos..."
  dotnet build "$SLNX" -c Release "${INCLUDE_FLAGS[@]}"
}

build_one() {
  local csproj
  csproj="$(resolve_csproj "$1")"
  echo "Building $1..."
  dotnet build "$csproj" -c Release "${INCLUDE_FLAGS[@]}"
}

run_one() {
  local name="$1"
  shift
  local csproj
  csproj="$(resolve_csproj "$name")"
  dotnet run --project "$csproj" -c Release "${INCLUDE_FLAGS[@]}" -- "$@"
}

clean_all() {
  echo "Cleaning all demos..."
  dotnet clean "$SLNX" -c Release -v quiet
  # Remove leftover bin/obj directories
  find "$SCRIPT_DIR" -maxdepth 3 \( -name bin -o -name obj \) -type d -exec rm -rf {} + 2>/dev/null || true
  echo "Done."
}

# --- Main ---

if [[ $# -eq 0 ]]; then
  build_all
  exit 0
fi

case "$1" in
  -h|--help|help)
    usage
    ;;
  list)
    list_demos
    ;;
  clean)
    clean_all
    ;;
  run)
    if [[ $# -lt 2 ]]; then
      echo "Error: 'run' requires a demo name." >&2
      echo "Usage: ./build.sh run <demo> [args]" >&2
      exit 1
    fi
    shift
    run_one "$@"
    ;;
  *)
    # Single demo name — just build it
    if [[ -n "${DEMOS[$1]:-}" ]]; then
      build_one "$1"
    else
      echo "Error: unknown command or demo '$1'" >&2
      echo "" >&2
      usage >&2
      exit 1
    fi
    ;;
esac

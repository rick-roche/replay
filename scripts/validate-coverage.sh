#!/bin/bash
# Validate code coverage thresholds
# This script checks if coverage meets the minimum thresholds and fails if not

MIN_LINE_THRESHOLD=${1:-54.5}
MIN_BRANCH_THRESHOLD=${2:-55.5}

# Find the most recent coverage file (portable across macOS and Linux)
COVERAGE_FILE=$(find ./TestResults -name "coverage.cobertura.xml" -type f | sort -r | head -1)

if [ -z "$COVERAGE_FILE" ]; then
    echo "Error: No coverage file found in ./TestResults"
    exit 1
fi

echo "Validating coverage from: $COVERAGE_FILE"
echo ""

# Extract coverage percentages from Cobertura XML
# Using grep and sed to parse the XML (avoiding dependency on external tools like xmllint)

LINE_RATE=$(grep -oE 'line-rate="[^"]*"' "$COVERAGE_FILE" | head -1 | grep -oE '"[^"]*"' | tr -d '"')
BRANCH_RATE=$(grep -oE 'branch-rate="[^"]*"' "$COVERAGE_FILE" | head -1 | grep -oE '"[^"]*"' | tr -d '"')

# Convert from decimal (0.544) to percentage (54.4)
LINE_COVERAGE=$(awk "BEGIN {printf \"%.1f\", $LINE_RATE * 100}")
BRANCH_COVERAGE=$(awk "BEGIN {printf \"%.1f\", $BRANCH_RATE * 100}")

echo "Coverage Results:"
echo "  Line coverage: ${LINE_COVERAGE}% (threshold: ${MIN_LINE_THRESHOLD}%)"
echo "  Branch coverage: ${BRANCH_COVERAGE}% (threshold: ${MIN_BRANCH_THRESHOLD}%)"
echo ""

FAILED=0

# Compare using awk to handle floating point comparison
if awk "BEGIN {exit !($LINE_COVERAGE < $MIN_LINE_THRESHOLD)}"; then
    echo "❌ FAILED: Line coverage ${LINE_COVERAGE}% is below threshold of ${MIN_LINE_THRESHOLD}%"
    FAILED=1
fi

if awk "BEGIN {exit !($BRANCH_COVERAGE < $MIN_BRANCH_THRESHOLD)}"; then
    echo "❌ FAILED: Branch coverage ${BRANCH_COVERAGE}% is below threshold of ${MIN_BRANCH_THRESHOLD}%"
    FAILED=1
fi

if [ $FAILED -eq 1 ]; then
    exit 1
fi

echo "✓ SUCCESS: All coverage thresholds met!"
exit 0

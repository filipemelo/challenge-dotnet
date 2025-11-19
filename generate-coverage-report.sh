#!/bin/bash
# Script to generate HTML coverage report

set -e

echo "🔍 Running tests with coverage..."
docker-compose exec web dotnet test src/Challenge.Tests/Challenge.Tests.csproj \
  --settings src/Challenge.Tests/Challenge.Tests.runsettings \
  --collect:"XPlat Code Coverage" \
  --verbosity minimal

echo ""
echo "📊 Generating HTML coverage report..."

# Find the latest coverage file
COVERAGE_FILE=$(docker-compose exec -T web bash -c 'find src/Challenge.Tests/TestResults -name "coverage.cobertura.xml" -type f -exec stat -c "%Y %n" {} \; | sort -rn | head -1 | cut -d" " -f2-')

if [ -z "$COVERAGE_FILE" ]; then
  echo "❌ Coverage file not found!"
  exit 1
fi

echo "Found coverage file: $COVERAGE_FILE"

# Generate HTML report
REPORT_DIR="src/Challenge.Tests/TestResults/coverage-report"
docker-compose exec web bash -c "
  reportgenerator \
    -reports:\"$COVERAGE_FILE\" \
    -targetdir:\"$REPORT_DIR\" \
    -reporttypes:Html \
    -classfilters:\"-*Migrations*\" \
    -filefilters:\"-*Migrations/**\"
  
  echo ''
  echo '✅ HTML coverage report generated!'
  echo '📁 Report location: $REPORT_DIR/index.html'
"

# Copy report to host
echo ""
echo "📋 Copying report to host..."
rm -rf ./coverage-report
CONTAINER_NAME=$(docker-compose ps --format "{{.Names}}" | grep web | head -1)
if [ -n "$CONTAINER_NAME" ]; then
  docker cp ${CONTAINER_NAME}:/app/src/Challenge.Tests/TestResults/coverage-report ./coverage-report 2>&1
  if [ -f "coverage-report/index.html" ]; then
    echo "✅ Report copied to: ./coverage-report/index.html"
    
    # Get coverage summary
    COVERAGE_SUMMARY=$(docker-compose exec -T web bash -c "
      COV_FILE=\"$COVERAGE_FILE\"
      if [ -f \"\$COV_FILE\" ]; then
        LINE_COV=\$(grep -oP 'line-rate=\"\K[0-9.]+' \"\$COV_FILE\" | head -1)
        TOTAL=\$(grep -oP 'lines-valid=\"\K[0-9]+' \"\$COV_FILE\")
        COVERED=\$(grep -oP 'lines-covered=\"\K[0-9]+' \"\$COV_FILE\")
        COV_PCT=\$(awk \"BEGIN {printf \\\"%.2f\\\", \$LINE_COV * 100}\")
        TARGET=\$(awk \"BEGIN {printf \\\"%.0f\\\", \$TOTAL * 0.8}\")
        REMAINING=\$(awk \"BEGIN {printf \\\"%.0f\\\", \$TARGET - \$COVERED}\")
        echo \"📊 Coverage: \${COV_PCT}% (\$COVERED/\$TOTAL linhas)\"
        echo \"🎯 Meta: 80.00% (\$TARGET linhas)\"
        if [ \$REMAINING -le 0 ]; then
          echo \"🎉 Meta de 80% atingida!\"
        else
          echo \"📈 Restante: \$REMAINING linhas\"
        fi
      fi
    ")
    
    echo ""
    echo "$COVERAGE_SUMMARY"
    echo ""
    echo "Excluded from report:"
    echo "  ✅ Program.cs"
    echo "  ✅ Migrations (ApplicationDbContextModelSnapshot, InitialCreate)"
    echo "  ✅ Views (*.cshtml)"
    echo ""
    echo "Opening report in browser..."
    open coverage-report/index.html
    echo "✅ Report opened in your default browser!"
  else
    echo "❌ Report file not found after copy"
    exit 1
  fi
else
  echo "❌ Container not found"
  exit 1
fi


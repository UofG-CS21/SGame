#!/bin/bash
# Launch SGame, run pytest tests, then kill SGame.
# Must be launched from ${CI_PROJECT_DIR}
# Usage: runtests.sh <host> <port>
SGAME_HOST=$1
SGAME_PORT=$2

# Verbose output
set -x

# Start an instance of the SGame server in the background (redirect stdout->stderr; sleep a bit for it to init)
# Store the PID of the background process in a file (SGame.pid) to know what process to kill after.
# (See issue #33 on why this is needed)
# Use sed to prefix "[SGame] " to the background process' output, for clarity
rm -f SGame.pid
dotnet run --project SGame -- --host ${SGAME_HOST} --port ${SGAME_PORT} 2>&1 | sed 's/^/[SGame] /' 1>&2 &
echo $! > SGame.pid
sleep 5

# Run the tests
pushd tests/
pytest *.py --sgame ${CI_PROJECT_DIR}/SGame --host ${SGAME_HOST} --port ${SGAME_PORT}
TESTS_EXIT_CODE=$?
popd

# Kill the background process in any case (tests succeeded or failure)
# (See issue #33; otherwise GitLab will stall until timeout because the SGame process in the background won't terminate!)
# First try to exit gracefully...
curl -X POST -d "exit" "http://${SGAME_HOST}:${SGAME_PORT}/exit" || echo "curl failed to POST exit command"
sleep 2
# ...then use brute force if the server still hasn't terminated
SGAME_PID=$(cat SGame.pid)
echo "Kill ${SGAME_PID}"
pkill -KILL ${SGAME_PID} || echo "Server already stopped"

exit $TESTS_EXIT_CODE

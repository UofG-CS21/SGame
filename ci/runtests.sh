#!/bin/bash
# Launch SGame, run pytest tests, then kill SGame.
# Must be launched from ${CI_PROJECT_DIR}
# Usage: runtests.sh <host> <port>
SGAME_HOST=$1
SGAME_PORT=$2

# Timeout before "Listnening..." is seen on the stdout of dotnet run
DOTNET_TIMEOUT=30

# Start an instance of the SGame server in the background (redirect stdout->stderr; sleep a bit for it to init)
# Store the PID of the background process in a file (SGame.pid) to know what process to kill after.
# (See issue #33 on why this is needed)
rm -f SGame.pid
dotnet run --project SGame -- --host ${SGAME_HOST} --port ${SGAME_PORT} &>SGame.out &
echo $! >SGame.pid
echo "Waiting for SGame to fully startup..."
if ! timeout ${DOTNET_TIMEOUT} bash ${CI_PROJECT_DIR}/ci/waitForServer.sh SGame.out; then
    echo "Timeout waiting for SGame to start"
    exit 1
fi
echo "SGame is listening"

# Run the tests
pushd tests/
pytest *.py --sgame ${CI_PROJECT_DIR}/SGame --host ${SGAME_HOST} --port ${SGAME_PORT}
TESTS_EXIT_CODE=$?
popd

# Kill the background process in any case (tests succeeded or failure)
# (See issue #33; otherwise GitLab will stall until timeout because the SGame process in the background won't terminate!)
# First try to exit gracefully...
echo "Send 'exit' command to SGame..."
curl -X POST -d "exit" "http://${SGAME_HOST}:${SGAME_PORT}/exit" || echo "curl failed to POST exit command"
sleep 2
# ...then use brute force if the server still hasn't terminated
SGAME_PID=$(cat SGame.pid)
echo "pkill SGame (PID=${SGAME_PID})"
pkill -KILL ${SGAME_PID} || echo "  SGame was already stopped"

echo "===== Server output ======================================================"
cat SGame.out
echo "=========================================================================="

# Adding in check to ensure that if the server crashes catch the error
wait %1
# Check if the error code is equal to 0 (Meaning that it has)
if [ $? -ne 0 ]
 then
    echo "SGame exited with error exit code"
    # Exit with this code 
    exit $?
fi

exit $TESTS_EXIT_CODE

#!/bin/bash
# Launch SGame, run pytest tests, then kill SGame.
# Must be launched from ${CI_PROJECT_DIR}
# Usage: runtests.sh <host> <port>
HOST=$1
SARBITER_PORT=$2
SGAME_PORT=9001
ELASTIC_URL="http://localhost:9200/"

function backgroundrun() {
    # backgroundrun <project> <args>
    rm -f $1.pid
    dotnet run --project $1 -- $2 &>$1.out &
    echo $! >$1.pid
    echo "Waiting for $1 to fully startup..."
    if ! timeout ${DOTNET_TIMEOUT} bash ${CI_PROJECT_DIR}/ci/waitForServer.sh $1.out; then
        echo "Timeout waiting for $1 to start"
        exit 1
    fi
    echo "$1 is listening"
}

function backgroundkill() {
    # backgroundkill <project> <port>
    # First try to exit gracefully...
    echo "Send 'exit' command to $1..."
    curl -X POST -d "exit" "http://${HOST}:$2/exit" || echo "curl failed to POST exit command"
    sleep 2
    # ...then use brute force if the server still hasn't terminated
    BG_PID=$(cat $1.pid)
    echo "pkill $1 (PID=${BG_PID})"
    pkill -KILL ${BG_PID} || echo "  $1 was already stopped"
}

# Timeout before "Listening..." is seen on the stdout of dotnet run
DOTNET_TIMEOUT=30

# Start an instance of the SArbiter server in the background (redirect stdout->stderr; sleep a bit for it to init)
# Store the PID of the background process in a file (SArbiter.pid) to know what process to kill after.
# (See issue #33 on why this is needed)
backgroundrun SArbiter "--api-url http://${HOST}:${SARBITER_PORT}/ --bus-port ${SARBITER_PORT}"

sleep 2

# Do the same for the SGame root node that manages the outermost quad
backgroundrun SGame "--api-url http://${HOST}:${SGAME_PORT}/ --local-bus-port ${SGAME_PORT} --arbiter-bus-port ${SARBITER_PORT} --persistence ${ELASTIC_URL}"

sleep 1

# Run the tests
pushd tests/
pytest *.py --sgame ${CI_PROJECT_DIR}/SGame --host ${HOST} --port ${SARBITER_PORT} --persistence ${ELASTIC_URL}
TESTS_EXIT_CODE=$?
popd

# Kill the background processes in any case (tests succeeded or failure)
# (See issue #33; otherwise GitLab will stall until timeout because the SGame process in the background won't terminate!)
backgroundkill SArbiter ${SARBITER_PORT}
backgroundkill SGame ${SGAME_PORT}

wait %1
if [ $? -ne 0 ]
then
    echo "SArbiter exited with error exit code"
    exit $?
fi
wait %2
if [ $? -ne 0 ]
then
    echo "SGame exited with error exit code"
    exit $?
fi

#rm -f {SGame,SArbiter}.{out,pid}
rm -f {SGame,SArbiter}.pid

exit $TESTS_EXIT_CODE

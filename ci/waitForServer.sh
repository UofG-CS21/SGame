#!/bin/bash
# Waits until the file at $1 reads "Listening", then exits
until grep -q "Listening" $1; do
    sleep 0.1
done

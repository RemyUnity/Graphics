#!/bin/sh

# Script that verifies that each branch follows the naming convention.

LC_ALL=C

echo "Checking that branch name follows the repository convention..."

local_branch="$(git rev-parse --abbrev-ref HEAD)"
valid_branch_regex="([a-z])+\/([a-z])+"
message="There is something wrong with your branch name. Branch names in this project must adhere to this contract: $valid_branch_regex (e.g. 'folder/something'). Your commit will be rejected. You should rename your branch to a valid name and try again."

if [[ ! $local_branch =~ $valid_branch_regex ]]
then
    echo "$message"
    exit 1
fi

echo "Completed."
exit 0 
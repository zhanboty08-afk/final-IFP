#!/usr/bin/env sh
set -eu

project_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
source_file="$project_root/BudgetPipeline/native/budget_native.c"
output_directory="$project_root/BudgetPipeline/runtimes/linux-x64/native"
output_file="$output_directory/libbudget_native.so"

mkdir -p "$output_directory"
cc -std=c11 -Wall -Wextra -Werror -O2 -fPIC -shared \
  "$source_file" -o "$output_file"

printf 'Built native library: %s\n' "$output_file"

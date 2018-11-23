#!/bin/bash

echo "Generating: $1"

cd $env:GOPATH/src/github.com/proepkes/speeddate/src/$1
goa gen github.com/proepkes/speeddate/src/$1/design
goa example github.com/proepkes/speeddate/src/$1/design

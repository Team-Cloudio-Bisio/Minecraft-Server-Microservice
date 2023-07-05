#!/bin/bash

property=$1	# property to change
newVal=$2	# value to assign to the property

sed -i "s/^${property}=.*/${property}=${newVal}/" server.properties

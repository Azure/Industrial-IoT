@echo off
setlocal

echo Processing Model Design Schema
xsd /classes /n:Opc.Ua.Design.Schema "UA Model Design.xsd"

echo #pragma warning disable 1591 > temp.txt
type "UA Model Design.cs" >> temp.txt
type temp.txt > "UA Model Design.cs"
del temp.txt

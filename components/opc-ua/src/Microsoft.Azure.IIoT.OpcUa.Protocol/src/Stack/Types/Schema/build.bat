@echo off
setlocal

echo Processing Type dictionary Schema
xsd /classes /n:Opc.Ua.Types.Schema "UA Type Dictionary.xsd"

echo #pragma warning disable 1591 > temp.txt
type "UA Type Dictionary.cs" >> temp.txt
type temp.txt > "UA Type Dictionary.cs"
del temp.txt

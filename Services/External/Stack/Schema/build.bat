@echo off
setlocal

echo Processing NodeSet Schema
xsd /classes /n:Opc.Ua.Export UANodeSet.xsd

echo #pragma warning disable 1591 > temp.txt
type UANodeSet.cs >> temp.txt
type temp.txt > UANodeSet.cs
del temp.txt
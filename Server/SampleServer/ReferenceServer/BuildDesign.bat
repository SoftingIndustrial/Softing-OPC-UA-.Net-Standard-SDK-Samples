@echo off
setlocal

SET PATH=%PATH%;.\Model

echo Building ModelDesign
Opc.Ua.ModelCompiler.exe -d2 ".\Model\ModelDesign.xml" -cg ".\Model\ModelDesign.csv" -o2 ".\Model"
echo Success!




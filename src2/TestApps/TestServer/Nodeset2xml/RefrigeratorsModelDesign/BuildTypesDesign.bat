@echo off
setlocal

SET PATH=%PATH%;..\..\..\..\..\bin\Net35;

echo Building ModelDesignRefrigerators
Softing.Opc.Ua.Sdk.ModelCompiler.exe -d2 ".\Types\ModelDesignRefrigerators.xml" -cg ".\Types\ModelDesignRefrigerators.csv" -o2 ".\Types"

echo Building ModelDesignRefrigeratorsInstances
Softing.Opc.Ua.Sdk.ModelCompiler.exe -d2 ".\Types\ModelDesignRefrigeratorsInstances.xml" -cg ".\Types\ModelDesignRefrigeratorsInstances.csv" -o2 ".\Types"
echo Success!




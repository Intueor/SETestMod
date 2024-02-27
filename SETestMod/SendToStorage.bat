set ModsFolder="\\SERVER\Mods"
set ModName="SETestMod"
mkdir %ModsFolder%\%ModName%\Data\Scripts\%ModName%
xcopy /Y .\SETestMod.cs %ModsFolder%\%ModName%\Data\Scripts\%ModName%
xcopy /Y .\thumb.png %ModsFolder%\%ModName%
xcopy /Y .\SETestMod.vdf %ModsFolder%
sudo rm -f /usr/local/bin/52wmovie
sudo rm -f /usr/local/bin/appsettings.json
cd ~/code/C#/52WeekMovieChallengeHelper/52WeekMovieChallengeHelper
dotnet clean
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
sudo cp bin/Release/net7.0/osx-arm64/publish/52WeekMovieChallengeHelper /usr/local/bin/52wmovie
sudo cp appsettings.json /usr/local/bin/
sudo chmod +x /usr/local/bin/52wmovie
hash -r

# 52wmovie
Start-Process -FilePath dotnet -NoNewWindow -Wait -WorkingDirectory "src/Polymerium.App/bin/Debug/net9.0" -ArgumentList "Polymerium.App.dll","--environment Development"

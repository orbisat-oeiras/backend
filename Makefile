.PHONY: install installhooks installdotnet

install: installhooks installdotnet

installhooks:
	cp githooks/* .git/hooks
	chmod +x .git/hooks/*

installdotnet:
	dotnet tool restore
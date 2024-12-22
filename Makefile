.PHONY: install install_hooks install_dotnet

install: install_hooks install_dotnet

install_hooks:
	cp githooks/* .git/hooks
	chmod +x .git/hooks/*

install_dotnet:
	dotnet tool restore
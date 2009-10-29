MONO =
MONOFLAGS =

all: pc.lexer.cs pc.parser.cs

pc.lexer.cs: pc.lexer
	$(MONO) $(MONOFLAGS) Tools/lg.exe pc.lexer

pc.parser.cs: pc.parser pc.lexer.cs
	$(MONO) $(MONOFLAGS) Tools/pg.exe pc.parser

clean:
	rm -f pc.lexer.cs pc.parser.cs *~

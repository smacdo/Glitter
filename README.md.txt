# Glitter
Glitter is a toy language exploring concepts in language design.

# Language
Glitter looks like this:

```
function fibonacci(n)
{
	if (n <= 1)
	{
		return n;
	}
	else
	{
		return fibonacci(n - 2) + fibonacci(n - 1);
	}
}

for (var i = 0; i < 10; i = i + 1)
{
	print "fib " + i + ": " + fibonacci(i);
}
```

TODO: Explain more about Glitter, including types supported, etc.

Glitter supports functions and closures as first class citizens:
```
function makeCounter()
{
	var counter = 0;

	function count()
	{
		counter = counter + 1;
		print counter;
	}

	return c;
}

var a = makeCounter();
a();			// "1"
a();			// "2"

var b = makeCounter();
b();			// "1"
```

# Future
The language, interpreter and compiler are in development. Among the many things that will be added are:
 - classes
 - arrays, strings, tables (hashes)
 - typing (language is gradually typed)
 - c++ interpreter and compiler once prototype is finished
 - assembly output for x86
grammar SqlSelect;
/*
 * Parser Rules
 */

parse
	: SELECT selectList
	  ( FROM collection )?
	  ( WHERE expr )?
	  ( GROUP BY groupByClause )? EOF
      ( ORDER BY orderByClause )? EOF
	;

collection
	: DEVICES
	| DEVICES_JOBS
	| DEVICES_MODULES
	;

maxCount
	: NUMERIC_LITERAL
	;

selectList
	: STAR
	| topExpr
    | ( topExpr )? selectExpr ( ',' selectExpr )*
	;

topExpr
	: TOP BR_OPEN maxCount BR_CLOSE
	;

selectExpr
	: ( columnName | aggregateExpr ) ( AS IDENTIFIER )?
	;

columnName
	: ( collection '.' )? propertyName
	;

propertyName
	: IDENTIFIER ( '.' IDENTIFIER )*
	;

aggregateExpr
	: COUNT BR_OPEN BR_CLOSE
	| ( SUM | AVG | MIN | MAX ) BR_OPEN columnName BR_CLOSE
	;

expr
	: BR_OPEN expr BR_CLOSE
	| NOT expr
	| expr AND expr
	| expr OR expr
	| columnName COMPARISON_OPERATOR ( columnName | array_literal | literal_value )
	| scalarFunction ( COMPARISON_OPERATOR literal_value )?
	;

scalarFunction
	: scalarTypeFunction '(' columnName ')'
	| ( STARTS_WITH | ENDS_WITH ) '(' columnName ',' STRING_LITERAL ')'
	;

scalarTypeFunction
	: IS_DEFINED
	| IS_NULL
	| IS_BOOL
	| IS_NUMBER
	| IS_STRING
	| IS_OBJECT
	| IS_PRIMITIVE
	;

array_literal
	: '[' ( literal_value ( ',' literal_value )* )? ']'
	;

literal_value
	: scalar_literal
	| object_literal
	;

object_literal
	: '{' ( keyValuePair ( ',' keyValuePair )* )? '}'
	;

keyValuePair
	: IDENTIFIER ':' ( scalar_literal | object_literal )
	;

scalar_literal
	: BOOLEAN
	| NUMERIC_LITERAL
	| STRING_LITERAL
	| NULL
	;

groupByClause
	: columnName ( ',' columnName )*
	;

orderByClause
	: sortColumn ( ',' sortColumn )*
	;

sortColumn
	: columnName ( ASC | DESC )?
	;

/*
 * Lexer Rules
 */

COMPARISON_OPERATOR
	: EQUALS
	| NOT_EQUALS
	| GREATER_THAN
	| GREATER_THAN_EQUALS
	| LESS_THAN
	| LESS_THAN_EQUALS
	| IN
	| NOT_IN
	| ALL
	;

BOOLEAN
	: TRUE
	| FALSE
	;

ALL: A L L;
AND: A N D;
AS: A S;
ASC: A S C;
AVG: A V G;
BY: B Y;
COUNT: C O U N T;
DESC: D E S C;
DEVICES_JOBS: D E V I C E S '.' J O B S;
DEVICES_MODULES: D E V I C E S '.' M O D U L E S;
DEVICES: D E V I C E S;
ENDS_WITH: E N D S W I T H;
FALSE: F A L S E;
FROM: F R O M;
GROUP: G R O U P;
IN: I N;
IS_BOOL: I S US B O O L;
IS_DEFINED: I S US D E F I N E D;
IS_NULL: I S US N U L L;
IS_NUMBER: I S US N U M B E R;
IS_OBJECT: I S US O B J E C T;
IS_PRIMITIVE: I S US P R I M I T I V E;
IS_STRING: I S US S T R I N G;
MAX: M A X;
MIN: M I N;
NOT: N O T;
NOT_IN: N I N;
NULL: N U L L;
OR: O R;
ORDER: O R D E R;
SELECT: S E L E C T;
STARTS_WITH: S T A R T S W I T H;
SUM: S U M;
TOP: T O P;
TRUE: T R U E;
WHERE: W H E R E;

BR_OPEN: '(';
BR_CLOSE: ')';
EQUALS: '=';
GREATER_THAN: '>';
GREATER_THAN_EQUALS: '>=';
LESS_THAN: '<';
LESS_THAN_EQUALS: '<=';
NOT_EQUALS: '!=' | '<>';
STAR: '*';

STRING_LITERAL
	: '\'' ( ~'\'' | '\'\'' )* '\''
	;

NUMERIC_LITERAL
	: ( '-' )? ( DIGIT* '.' )? DIGIT+
	;

IDENTIFIER
	: ( '$' )? ( ~[ .,'"!=<>*$#:(){}[\]\t\r\n] )+
	| '[[' ( '$' )? ( ~[ .$#[\]\t\r\n] )+ ']]'
	;

fragment US: '_';

fragment DIGIT: [0-9];

fragment A : [Aa];
fragment B : [Bb];
fragment C : [Cc];
fragment D : [Dd];
fragment E : [Ee];
fragment F : [Ff];
fragment G : [Gg];
fragment H : [Hh];
fragment I : [Ii];
fragment J : [Jj];
fragment K : [Kk];
fragment L : [Ll];
fragment M : [Mm];
fragment N : [Nn];
fragment O : [Oo];
fragment P : [Pp];
fragment Q : [Qq];
fragment R : [Rr];
fragment S : [Ss];
fragment T : [Tt];
fragment U : [Uu];
fragment V : [Vv];
fragment W : [Ww];
fragment X : [Xx];
fragment Y : [Yy];
fragment Z : [Zz];

WS
	:	[ \t\r\n] -> channel(HIDDEN)
	;

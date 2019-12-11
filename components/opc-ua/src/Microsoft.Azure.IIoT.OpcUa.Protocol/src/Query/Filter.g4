grammar Filter;


parse
	: SELECT selectList
	  ( WHERE elem_op )? EOF
	;

selectList
	: STAR
    | selectexpr ( ',' selectexpr )*
	;

selectexpr
	: attr_op ( AS IDENTIFIER )?
	;

/*
  Operands
*/

elem_op
	: BR_OPEN elem_op BR_CLOSE
	| NOT ( attr_op | elem_op )
	| elem_op AND elem_op
	| elem_op OR elem_op
	| elem_op COMPARISON_OPERATOR ( lit_op | attr_op )
	| attr_op COMPARISON_OPERATOR ( lit_op | attr_op )
    | elem_op BETWEEN ( range )
    | attr_op BETWEEN ( range )
    | elem_op RELATED_TO ( relation )
    | attr_op RELATED_TO ( relation )
	| elem_op IN ( attr_op | list )
	| attr_op IN ( attr_op | list )
	| SCALAR_FUNCTION '(' ( attr_op | elem_op ) ')' ( COMPARISON_OPERATOR lit_op )?
	;

lit_op
    : BR_OPEN lit_op BR_CLOSE
	| scalar
    | array
	;

attr_op
    : BR_OPEN attr_op BR_CLOSE
	| nodeId ( '.' attributeId )?
	;

/*
  Literals
*/

list
	: '[' ( scalar ( ',' scalar )* )? ']'
	;

range
	: BR_OPEN ( NUMERIC_LITERAL ',' NUMERIC_LITERAL ) BR_CLOSE
	;

relation
	: BR_OPEN ( ( lit_op | attr_op ) ( ',' ( lit_op | attr_op ) )* ) BR_CLOSE
	;

scalar
	: BOOLEAN
	| NUMERIC_LITERAL
	| STRING_LITERAL
	| NULL
	;

array
	: '[' ( lit_op ( ',' lit_op )* )? ']'
	;

nodeId
    : BR_OPEN  STRING_LITERAL ( ',' browsePathElement )* BR_CLOSE
    | STRING_LITERAL
	;

browsePathElement
    : BR_OPEN browsePathElement BR_CLOSE
    | STRING_LITERAL
	;

attributeId
    : IDENTIFIER
    | NUMERIC_LITERAL
    ;

/*
  Tokens
*/

TRUE: T R U E;
FALSE: F A L S E;

BOOLEAN
	: TRUE
	| FALSE
	;

NULL: N U L L;

SELECT: S E L E C T;
STAR: '*';
WHERE: W H E R E;
AS: A S;

SCALAR_FUNCTION
	: IS_NULL
    | OF_TYPE
    | IN_VIEW
	;

COMPARISON_OPERATOR
	: EQUALS
	| NOT_EQUALS
    | LIKE
	| GREATER_THAN
	| GREATER_THAN_EQUALS
	| LESS_THAN
	| LESS_THAN_EQUALS
    | BITWISE_AND
    | BITWISE_OR
	;

EQUALS: '=';
GREATER_THAN: '>';
GREATER_THAN_EQUALS: '>=';
LESS_THAN: '<';
LESS_THAN_EQUALS: '<=';
BITWISE_AND: '&';
BITWISE_OR: '|';
NOT_EQUALS: '!=' | '<>';

IN: I N;
BETWEEN: B E T W E E N;
LIKE: L I K E;

NOT: N O T;
OR: O R;
AND: A N D;

OF_TYPE: O F US T Y P E;
RELATED_TO: R E L A T E D US T O;
IN_VIEW: I N US V I E W;
IS_NULL: I S US N U L L;

AVG: A V G;
MAX: M A X;
MIN: M I N;
SUM: S U M;
COUNT: C O U N T;

BR_OPEN: '(';
BR_CLOSE: ')';

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

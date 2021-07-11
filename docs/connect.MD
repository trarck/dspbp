A:0 -> 1:B 
A:fromObjId=A 		toObjId=B		fromSlot=0		toSlot=1		isOutput=true
B:fromObjId=B		toObjId=A		fromSlot=1		toSlot=0		isOutput=false

C:0 -> 2:B
C:fromObjId=C 		toObjId=B		fromSlot=0		toSlot=2		isOutput=true
B:fromObjId=B		toObjId=C		fromSlot=2		toSlot=0		isOutput=false

A:startSlot -> 1:B:0->endSlot:C
B(input):fromObjId=B		toObjId=A		fromSlot=1		toSlot=startSlot		isOutput=false
B(output):fromObjId=B		toObjId=C		fromSlot=0		toSlot=endSlot			isOutput=true
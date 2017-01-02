# [LeetCode](https://leetcode.com/problemset/algorithms/?cong=true "LeetCode")
## Java语言
* Main.java         : 主函数
* Solution.java     : 解决方法

## 目录

* [双指针算法](https://github.com/AeroYoung/LeetCode/blob/master/%E5%8F%8C%E6%8C%87%E9%92%88.md)——No15,No16,No18.
* [Breadth-frist search](https://github.com/AeroYoung/LeetCode/blob/master/Breadth%20First%20Search%20%E5%B9%BF%E5%BA%A6%E4%BC%98%E5%85%88%E6%90%9C%E7%B4%A2%E7%AE%97%E6%B3%95.md)
* [No.11Container With Most Water](https://github.com/AeroYoung/LeetCode/issues/1)
* [No.14 Longest Common Prefix](https://github.com/AeroYoung/LeetCode/issues/2)

## 未归类问题

### [No65. Valid Number](https://leetcode.com/problems/valid-number/)
>Validate if a given string is numeric.
>Some examples:
>"0" => true
>" 0.1 " => true
>"abc" => false
>"1 a" => false
>"2e10" => true
>**Note: **It is intended for the problem statement to be ambiguous. You should gather all requirements up front before implementing one. 

先看看[java正则表达式](http://www.runoob.com/java/java-regular-expressions.html)

We start with trimming.
If we see [0-9] we reset the number flags.
We can only see . if we didn't see e or ..
We can only see e if we didn't see e but we did see a number. We reset numberAfterE flag.
We can only see + and - in the beginning and after an e.
any other character break the validation.
At the and it is only valid if there was at least 1 number and if we did see an e then a number after it as well.

So basically the number should match this regular expression:

[-+]?(([0-9]+(.[0-9]*)?)|.[0-9]+)(e[-+]?[0-9]+)?

```java

public boolean isNumber(String s) {
	s = s.trim();
    
    boolean pointSeen = false;
    boolean eSeen = false;
    boolean numberSeen = false;
    boolean numberAfterE = true;
    for(int i=0; i<s.length(); i++) {
        if('0' <= s.charAt(i) && s.charAt(i) <= '9') {
            numberSeen = true;
            numberAfterE = true;
        } else if(s.charAt(i) == '.') {
            if(eSeen || pointSeen) {
                return false;
            }
            pointSeen = true;
        } else if(s.charAt(i) == 'e') {
            if(eSeen || !numberSeen) {
                return false;
            }
            numberAfterE = false;
            eSeen = true;
        } else if(s.charAt(i) == '-' || s.charAt(i) == '+') {
            if(i != 0 && s.charAt(i-1) != 'e') {
                return false;
            }
        } else {
            return false;
        }
    }
    
    return numberSeen && numberAfterE;
}

```

### [NoNo8. String to Integer (atoi)](https://leetcode.com/problems/string-to-integer-atoi/)

```java

public int myAtoi(String str) {
    int index = 0, sign = 1, total = 0;
    //1. Empty string
    if(str.length() == 0) return 0;

    //2. Remove Spaces
    while(str.charAt(index) == ' ' && index < str.length())
        index ++;

    //3. Handle signs
    if(str.charAt(index) == '+' || str.charAt(index) == '-'){
        sign = str.charAt(index) == '+' ? 1 : -1;
        index ++;
    }
    
    //4. Convert number and avoid overflow
    while(index < str.length()){
        int digit = str.charAt(index) - '0';
        if(digit < 0 || digit > 9) break;

        //check if total will be overflow after 10 times and add digit
        if(Integer.MAX_VALUE/10 < total || Integer.MAX_VALUE/10 == total && Integer.MAX_VALUE %10 < digit)
            return sign == 1 ? Integer.MAX_VALUE : Integer.MIN_VALUE;

        total = 10 * total + digit;
        index ++;
    }
    return total * sign;
}

```


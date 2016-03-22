<%  
Set fs = CreateObject("Scripting.FileSystemObject") 
Wfile="e:\wwwroot\my\counter.dat" 
on error resume next 
Set a = fs.OpenTextFile(Wfile) 
hits = Clng(a.ReadLine) 
hits = hits + 1 
a.close
if error then 
hits = 2 
end if

Set a = fs.CreateTextFile(Wfile, True) 
a.WriteLine(hits) 
a.Close 
%> 

Return: <% =hits %>

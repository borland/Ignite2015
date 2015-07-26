file = File.open 'test.txt', 'w'
file.write "Hello World 2"
file.close

file = File.open 'test.txt', 'r'
puts file.read
file.close

File.open 'test.txt', 'w' do |f|
  f.write "Hello World 3"
end

File.open 'test.txt', 'r' do |f|
  puts f.read
end
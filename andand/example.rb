require_relative 'andand'

class Token
  attr_accessor :name

  def initialize(name)
    @name = name
  end

  def to_s
    "T:#{@name}"
  end
end

def load(filename)
  return "Serialized tokens"
end

def read(file) # returns array of tokens
  return [Token.new('t1'), Token.new('t2')]
end

matched_token = nil

if contents = load("file.txt")
  if tokens = read(contents)
    matched_token = tokens.detect{ |t| t.name == 't1' }
  end
end

puts "1: matched token #{matched_token}"

# converts to

if contents = load("file.txt") && tokens = read(contents)
  matched_token = tokens.detect{ |t| t.name == 't1' }
end

puts "2: matched token #{matched_token}"

# converts to

matched_token = (contents = load("file.txt") && tokens = read(contents) && tokens.detect{ |t| t.name == 't1' })

puts "3: matched token #{matched_token}"

matched_token = load("file.txt").andand{ |data| read(data) }.andand.detect{ |t| t.name == 't1' }

puts "4: matched token #{matched_token}"
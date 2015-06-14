class AndAndObjectWrapper
  def initialize(x)
    @x = x
  end

  def method_missing(sym, *args, &block)
    @x ? @x.send(sym, *args, &block) : nil
  end
end

class Object
  def andand(&block)
    return block.call(self) if block
    return AndAndObjectWrapper.new(self)
  end
end
